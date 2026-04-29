using Digiprise.PMS.Contracts.DTOs;
using Digiprise.PMS.Contracts.Requests;

namespace Digiprise.PMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthTokenDto?> LoginAsync(string email, string password, int tenantId, CancellationToken ct = default);
    Task<AuthTokenDto?> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<UserDto?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}

public interface IProjectService
{
    Task<ProjectDto?> GetByIdAsync(int projectId, int tenantId, CancellationToken ct = default);
    Task<ProjectDto?> GetByKeyAsync(string key, int tenantId, CancellationToken ct = default);
    Task<IEnumerable<ProjectDto>> GetAllAsync(int tenantId, CancellationToken ct = default);
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, int tenantId, int currentUserId, CancellationToken ct = default);
    Task<ProjectDto> UpdateAsync(int projectId, UpdateProjectRequest request, int tenantId, CancellationToken ct = default);
    Task ArchiveAsync(int projectId, int tenantId, CancellationToken ct = default);
    Task DeleteAsync(int projectId, int tenantId, CancellationToken ct = default);
    Task AddMemberAsync(int projectId, AddMemberRequest request, int tenantId, CancellationToken ct = default);
    Task RemoveMemberAsync(int projectId, int userId, int tenantId, CancellationToken ct = default);
}

public interface IIssueService
{
    Task<IssueDto?> GetByIdAsync(int issueId, int tenantId, CancellationToken ct = default);
    Task<IEnumerable<IssueListItemDto>> GetByProjectAsync(int projectId, int tenantId, CancellationToken ct = default);
    Task<IEnumerable<IssueListItemDto>> GetBacklogAsync(int projectId, int tenantId, CancellationToken ct = default);
    Task<IEnumerable<IssueListItemDto>> SearchAsync(IssueSearchRequest request, int tenantId, CancellationToken ct = default);
    Task<IssueDto> CreateAsync(CreateIssueRequest request, int tenantId, int currentUserId, CancellationToken ct = default);
    Task<IssueDto> UpdateAsync(int issueId, UpdateIssueRequest request, int tenantId, int currentUserId, CancellationToken ct = default);
    Task TransitionAsync(int issueId, TransitionIssueRequest request, int tenantId, int currentUserId, CancellationToken ct = default);
    Task DeleteAsync(int issueId, int tenantId, int currentUserId, CancellationToken ct = default);
    Task BulkUpdateAsync(BulkIssueRequest request, int tenantId, int currentUserId, CancellationToken ct = default);
    Task<CommentDto> AddCommentAsync(int issueId, CreateCommentRequest request, int tenantId, int currentUserId, CancellationToken ct = default);
    Task<IEnumerable<CommentDto>> GetCommentsAsync(int issueId, int tenantId, CancellationToken ct = default);
}

public interface ISprintService
{
    Task<SprintDto?> GetByIdAsync(int sprintId, CancellationToken ct = default);
    Task<IEnumerable<SprintDto>> GetByProjectAsync(int projectId, CancellationToken ct = default);
    Task<SprintDto?> GetActiveSprintAsync(int boardId, CancellationToken ct = default);
    Task<SprintDto> CreateAsync(int projectId, CreateSprintRequest request, CancellationToken ct = default);
    Task<SprintDto> StartAsync(int sprintId, StartSprintRequest request, int currentUserId, CancellationToken ct = default);
    Task<SprintDto> CloseAsync(int sprintId, int currentUserId, CancellationToken ct = default);
    Task<IEnumerable<BurndownPointDto>> GetBurndownAsync(int sprintId, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int tenantId, int userId, CancellationToken ct = default);
    Task<IEnumerable<IssueListItemDto>> GetAssignedToMeAsync(int userId, int tenantId, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId, CancellationToken ct = default);
    Task MarkReadAsync(int notificationId, int userId, CancellationToken ct = default);
    Task MarkAllReadAsync(int userId, CancellationToken ct = default);
    Task SendAsync(int userId, int tenantId, string title, string body, string? entityType = null, int? entityId = null, CancellationToken ct = default);
}

public interface ICurrentUserContext
{
    int UserId { get; }
    int TenantId { get; }
    string Email { get; }
    string[] Roles { get; }
    bool IsAdmin { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtService
{
    string GenerateAccessToken(int userId, int tenantId, string email, string[] roles);
    string GenerateRefreshToken();
    (int userId, int tenantId) ValidateAccessToken(string token);
}

public interface IEventBus
{
    Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : class;
}
