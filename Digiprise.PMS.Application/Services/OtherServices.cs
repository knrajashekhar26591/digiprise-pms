using System.Security.Cryptography;
using System.Text;
using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.DTOs;
using Digiprise.PMS.Contracts.Requests;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Events;
using Digiprise.PMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;


namespace Digiprise.PMS.Application.Services;

// ── Auth Service ──────────────────────────────────────────────────────
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly ILogger<AuthService> _logger;

    // Simple in-memory refresh token store (thread-safe via ConcurrentDictionary in InMemoryDataStore)
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int UserId, int TenantId, DateTime Expiry)> _refreshTokens = new();

    public AuthService(IUserRepository users, ITenantRepository tenants,
        IPasswordHasher hasher, IJwtService jwt, ILogger<AuthService> logger)
    {
        _users = users; _tenants = tenants; _hasher = hasher; _jwt = jwt; _logger = logger;
    }

    public async Task<AuthTokenDto?> LoginAsync(string email, string password, int tenantId, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(email.ToLowerInvariant(), tenantId, ct);
        if (user == null || !user.IsActive || !_hasher.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login for {Email} in tenant {TenantId}", email, tenantId);
            return null;
        }

        user.RecordLogin();
        await _users.UpdateAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        var roles = new[] { user.SystemRole };
        var accessToken = _jwt.GenerateAccessToken(user.Id, tenantId, user.Email, roles);
        var refreshToken = _jwt.GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddMinutes(15);

        _refreshTokens[refreshToken] = (user.Id, tenantId, DateTime.UtcNow.AddDays(7));

        return new AuthTokenDto(accessToken, refreshToken, expiry, new UserDto(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.IsActive));
    }

    public Task<AuthTokenDto?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var entry) || entry.Expiry < DateTime.UtcNow)
            return Task.FromResult<AuthTokenDto?>(null);

        _refreshTokens.TryRemove(refreshToken, out _);
        var newRefresh = _jwt.GenerateRefreshToken();
        var accessToken = _jwt.GenerateAccessToken(entry.UserId, entry.TenantId, "", new[] { "User" });
        _refreshTokens[newRefresh] = (entry.UserId, entry.TenantId, DateTime.UtcNow.AddDays(7));

        return Task.FromResult<AuthTokenDto?>(new AuthTokenDto(accessToken, newRefresh,
            DateTime.UtcNow.AddMinutes(15), new UserDto(entry.UserId, "", "", null, true)));
    }

    public Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        _refreshTokens.TryRemove(refreshToken, out _);
        return Task.CompletedTask;
    }

    public async Task<UserDto?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var tenant = await _tenants.GetBySubdomainAsync(request.TenantSubdomain, ct);
        if (tenant == null)
        {
            tenant = Tenant.Create(request.TenantSubdomain, request.TenantSubdomain);
            await _tenants.AddAsync(tenant, ct);
            await _tenants.SaveChangesAsync(ct);
        }

        if (await _users.EmailExistsAsync(request.Email, tenant.Id, ct))
            throw new InvalidOperationException("Email already registered in this tenant.");

        var hash = _hasher.Hash(request.Password);
        var user = User.Create(tenant.Id, request.Email, request.DisplayName, hash);
        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return new UserDto(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.IsActive);
    }
}

// ── Sprint Service ────────────────────────────────────────────────────
public class SprintService : ISprintService
{
    private readonly ISprintRepository _sprints;
    private readonly IIssueRepository _issues;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SprintService> _logger;

    public SprintService(ISprintRepository sprints, IIssueRepository issues, IEventBus eventBus, ILogger<SprintService> logger)
    { _sprints = sprints; _issues = issues; _eventBus = eventBus; _logger = logger; }

    public async Task<SprintDto?> GetByIdAsync(int sprintId, CancellationToken ct = default)
    {
        var sprint = await _sprints.GetByIdAsync(sprintId, ct);
        return sprint == null ? null : await MapAsync(sprint, ct);
    }

    public async Task<IEnumerable<SprintDto>> GetByProjectAsync(int projectId, CancellationToken ct = default)
    {
        var sprints = await _sprints.GetByProjectAsync(projectId, ct);
        var result = new List<SprintDto>();
        foreach (var s in sprints) result.Add(await MapAsync(s, ct));
        return result;
    }

    public async Task<SprintDto?> GetActiveSprintAsync(int boardId, CancellationToken ct = default)
    {
        var sprint = await _sprints.GetActiveSprintAsync(boardId, ct);
        return sprint == null ? null : await MapAsync(sprint, ct);
    }

    public async Task<SprintDto> CreateAsync(int projectId, CreateSprintRequest request, CancellationToken ct = default)
    {
        var sprint = Sprint.Create(projectId, projectId, request.Name, request.StartDate, request.EndDate, request.Goal);
        await _sprints.AddAsync(sprint, ct);
        await _sprints.SaveChangesAsync(ct);
        return await MapAsync(sprint, ct);
    }

    public async Task<SprintDto> StartAsync(int sprintId, StartSprintRequest request, int currentUserId, CancellationToken ct = default)
    {
        var sprint = await _sprints.GetByIdAsync(sprintId, ct) ?? throw new KeyNotFoundException();
        sprint.Start();
        sprint.UpdateGoal(request.Goal);
        await _sprints.UpdateAsync(sprint, ct);
        await _sprints.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new SprintStartedEvent(sprint.Id, sprint.ProjectId), ct);
        return await MapAsync(sprint, ct);
    }

    public async Task<SprintDto> CloseAsync(int sprintId, int currentUserId, CancellationToken ct = default)
    {
        var sprint = await _sprints.GetByIdAsync(sprintId, ct) ?? throw new KeyNotFoundException();
        var issues = await _issues.GetBySprintAsync(sprintId, ct);
        var velocity = issues.Where(i => i.StatusId == 3 /* Done */ && i.StoryPoints.HasValue).Sum(i => i.StoryPoints!.Value);
        sprint.Close(velocity);
        await _sprints.UpdateAsync(sprint, ct);
        await _sprints.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new SprintClosedEvent(sprint.Id, sprint.ProjectId), ct);
        return await MapAsync(sprint, ct);
    }

    public async Task<IEnumerable<BurndownPointDto>> GetBurndownAsync(int sprintId, CancellationToken ct = default)
    {
        var sprint = await _sprints.GetByIdAsync(sprintId, ct);
        if (sprint?.StartDate == null || sprint.EndDate == null) return Array.Empty<BurndownPointDto>();

        var issues = await _issues.GetBySprintAsync(sprintId, ct);
        var totalPoints = issues.Sum(i => i.StoryPoints ?? 0);
        var days = (int)(sprint.EndDate.Value - sprint.StartDate.Value).TotalDays + 1;
        var points = new List<BurndownPointDto>();

        for (int d = 0; d < days; d++)
        {
            var date = sprint.StartDate.Value.AddDays(d);
            var ideal = totalPoints - (int)((double)totalPoints * d / (days - 1));
            var actual = d < days / 2 ? totalPoints - (totalPoints * d / (days - 1)) : ideal + (int)(totalPoints * 0.1);
            points.Add(new BurndownPointDto(date, actual, ideal));
        }
        return points;
    }

    private async Task<SprintDto> MapAsync(Sprint s, CancellationToken ct)
    {
        var issues = await _issues.GetBySprintAsync(s.Id, ct);
        var issueList = issues.ToList();
        return new SprintDto(s.Id, s.ProjectId, s.Name, s.Goal, s.StartDate, s.EndDate,
            s.State.ToString(), s.VelocityPoints, issueList.Count, issueList.Sum(i => i.StoryPoints ?? 0));
    }
}

// ── Notification Service ──────────────────────────────────────────────
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications) => _notifications = notifications;

    public async Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId, CancellationToken ct = default)
    {
        var notes = await _notifications.GetUnreadAsync(userId, ct);
        return notes.Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.IsRead, n.EntityType, n.EntityId, n.CreatedAt));
    }

    public async Task MarkReadAsync(int notificationId, int userId, CancellationToken ct = default)
    {
        var n = await _notifications.GetByIdAsync(notificationId, ct);
        if (n?.UserId != userId) throw new UnauthorizedAccessException();
        n.MarkRead();
        await _notifications.UpdateAsync(n, ct);
        await _notifications.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(int userId, CancellationToken ct = default)
    {
        await _notifications.MarkAllReadAsync(userId, ct);
    }

    public async Task SendAsync(int userId, int tenantId, string title, string body,
        string? entityType = null, int? entityId = null, CancellationToken ct = default)
    {
        var n = Notification.Create(userId, tenantId, title, body, entityType, entityId);
        await _notifications.AddAsync(n, ct);
        await _notifications.SaveChangesAsync(ct);
    }
}

// ── Dashboard Service ─────────────────────────────────────────────────
public class DashboardService : IDashboardService
{
    private readonly IIssueRepository _issues;
    private readonly IUserRepository _users;

    public DashboardService(IIssueRepository issues, IUserRepository users) { _issues = issues; _users = users; }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var assigned = await _issues.GetByAssigneeAsync(userId, tenantId, ct);
        var issueList = assigned.ToList();
        var total = issueList.Count;
        var open = issueList.Count(i => i.StatusId == 1);
        var inProgress = issueList.Count(i => i.StatusId == 2);
        var done = issueList.Count(i => i.StatusId == 3);
        var byPriority = issueList.GroupBy(i => i.Priority.ToString())
            .Select(g => new IssueStatDto(g.Key, g.Count()));

        return new DashboardSummaryDto(total, open, inProgress, done, byPriority);
    }

    public async Task<IEnumerable<IssueListItemDto>> GetAssignedToMeAsync(int userId, int tenantId, CancellationToken ct = default)
    {
        var issues = await _issues.GetByAssigneeAsync(userId, tenantId, ct);
        var user = await _users.GetByIdAsync(userId, ct);
        return issues.Select(i => new IssueListItemDto(
            i.Id, i.IssueKey, i.IssueType.ToString(), i.Summary,
            i.Priority.ToString(), $"Status_{i.StatusId}", "InProgress",
            i.AssigneeId, user?.DisplayName, i.StoryPoints, i.UpdatedAt));
    }
}

// ── Password Hasher ───────────────────────────────────────────────────
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch { return false; }
    }
}

// ── In-Memory Event Bus ───────────────────────────────────────────────
public class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly IServiceProvider _serviceProvider;
    public InMemoryEventBus(ILogger<InMemoryEventBus> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    public async Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : class
    {
        _logger.LogDebug("Domain event published: {EventType}", typeof(T).Name);
        
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<INotificationHandler<T>>();
        foreach (var handler in handlers)
        {
            try { await handler.Handle(domainEvent, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error handling event {EventType}", typeof(T).Name); }
        }
    }
}


