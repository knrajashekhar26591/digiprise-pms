using System.Collections.Concurrent;
using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using Digiprise.PMS.Domain.Interfaces;
using Digiprise.PMS.Infrastructure.Data;

namespace Digiprise.PMS.Infrastructure.Repositories;

// ── Generic In-Memory Repository ──────────────────────────────────────
public abstract class InMemoryRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ConcurrentDictionary<int, T> _store;
    private readonly InMemoryDataStore _db;

    protected InMemoryRepository(ConcurrentDictionary<int, T> store, InMemoryDataStore db)
    { _store = store; _db = db; }

    public Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var v) ? v : null);

    public Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IEnumerable<T>>(_store.Values.ToList());

    public Task AddAsync(T entity, CancellationToken ct = default)
    {
        var id = _db.NextId();
        InMemoryDataStore.SetId(entity, id);
        _store[id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    { _store[entity.Id] = entity; return Task.CompletedTask; }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    { _store.TryRemove(entity.Id, out _); return Task.CompletedTask; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
}

// ── Project Repository ────────────────────────────────────────────────
public class ProjectRepository : InMemoryRepository<Project>, IProjectRepository
{
    public ProjectRepository(InMemoryDataStore db) : base(db.Projects, db) { }

    public Task<Project?> GetByKeyAsync(int tenantId, string key, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(p => p.TenantId == tenantId && p.Key == key));

    public Task<IEnumerable<Project>> GetByTenantAsync(int tenantId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Project>>(_store.Values
            .Where(p => p.TenantId == tenantId && p.Status != ProjectStatus.Deleted)
            .OrderByDescending(p => p.UpdatedAt).ToList());

    public Task<bool> KeyExistsAsync(int tenantId, string key, CancellationToken ct = default)
        => Task.FromResult(_store.Values.Any(p => p.TenantId == tenantId && p.Key == key));
}

// ── Issue Repository ──────────────────────────────────────────────────
public class IssueRepository : InMemoryRepository<Issue>, IIssueRepository
{
    public IssueRepository(InMemoryDataStore db) : base(db.Issues, db) { }

    public Task<Issue?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => GetByIdAsync(id, ct); // In-memory already has full object graph

    public Task<IEnumerable<Issue>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Issue>>(_store.Values
            .Where(i => i.ProjectId == projectId)
            .OrderByDescending(i => i.UpdatedAt).ToList());

    public Task<IEnumerable<Issue>> GetBySprintAsync(int sprintId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Issue>>(_store.Values.Where(i => i.SprintId == sprintId).ToList());

    public Task<IEnumerable<Issue>> GetBacklogAsync(int projectId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Issue>>(_store.Values
            .Where(i => i.ProjectId == projectId && i.SprintId == null)
            .OrderByDescending(i => i.Priority).ToList());

    public Task<IEnumerable<Issue>> GetByAssigneeAsync(int userId, int tenantId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Issue>>(_store.Values
            .Where(i => i.AssigneeId == userId && i.TenantId == tenantId)
            .OrderByDescending(i => i.UpdatedAt).ToList());

    public Task<string> GenerateIssueKeyAsync(int projectId, string projectKey, CancellationToken ct = default)
    {
        var existing = _store.Values.Where(i => i.ProjectId == projectId).Select(i => i.IssueKey)
            .Select(k => { var parts = k.Split('-'); return parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0; })
            .DefaultIfEmpty(0).Max();
        return Task.FromResult($"{projectKey}-{existing + 1}");
    }

    public Task<IEnumerable<Issue>> SearchByIqlAsync(string iql, int tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _store.Values.Where(i => i.TenantId == tenantId).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(iql))
        {
            var lower = iql.ToLowerInvariant();
            if (lower.Contains("issuetype = bug"))       query = query.Where(i => i.IssueType == IssueType.Bug);
            if (lower.Contains("issuetype = story"))     query = query.Where(i => i.IssueType == IssueType.Story);
            if (lower.Contains("issuetype = epic"))      query = query.Where(i => i.IssueType == IssueType.Epic);
            if (lower.Contains("priority = blocker"))    query = query.Where(i => i.Priority == Priority.Blocker);
            if (lower.Contains("priority = critical"))   query = query.Where(i => i.Priority == Priority.Critical);
            if (lower.Contains("sprint = opensprints()")) query = query.Where(i => i.SprintId != null);
        }
        return Task.FromResult<IEnumerable<Issue>>(query.OrderByDescending(i => i.UpdatedAt).Skip(page * pageSize).Take(pageSize).ToList());
    }

    public Task<Dictionary<int, string>> GetJournalsAtBaselineAsync(IEnumerable<int> issueIds, DateTimeOffset baseline, CancellationToken ct = default)
    {
        return Task.FromResult(new Dictionary<int, string>());
    }
}

// ── Sprint Repository ─────────────────────────────────────────────────
public class SprintRepository : InMemoryRepository<Sprint>, ISprintRepository
{
    public SprintRepository(InMemoryDataStore db) : base(db.Sprints, db) { }

    public Task<Sprint?> GetActiveSprintAsync(int boardId, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(s => s.BoardId == boardId && s.State == SprintState.Active));

    public Task<IEnumerable<Sprint>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Sprint>>(_store.Values.Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CreatedAt).ToList());
}

// ── User Repository ───────────────────────────────────────────────────
public class UserRepository : InMemoryRepository<User>, IUserRepository
{
    public UserRepository(InMemoryDataStore db) : base(db.Users, db) { }

    public Task<User?> GetByEmailAsync(string email, int tenantId, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(u => u.Email == email.ToLowerInvariant() && u.TenantId == tenantId));

    public Task<IEnumerable<User>> GetByTenantAsync(int tenantId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<User>>(_store.Values.Where(u => u.TenantId == tenantId && u.IsActive).ToList());

    public Task<bool> EmailExistsAsync(string email, int tenantId, CancellationToken ct = default)
        => Task.FromResult(_store.Values.Any(u => u.Email == email.ToLowerInvariant() && u.TenantId == tenantId));
}

// ── Tenant Repository ─────────────────────────────────────────────────
public class TenantRepository : InMemoryRepository<Tenant>, ITenantRepository
{
    public TenantRepository(InMemoryDataStore db) : base(db.Tenants, db) { }

    public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(t => t.Subdomain == subdomain.ToLowerInvariant()));
}

// ── Notification Repository ───────────────────────────────────────────
public class NotificationRepository : InMemoryRepository<Notification>, INotificationRepository
{
    public NotificationRepository(InMemoryDataStore db) : base(db.Notifications, db) { }

    public Task<IEnumerable<Notification>> GetUnreadAsync(int userId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Notification>>(_store.Values
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt).Take(50).ToList());

    public Task MarkAllReadAsync(int userId, CancellationToken ct = default)
    {
        foreach (var n in _store.Values.Where(n => n.UserId == userId && !n.IsRead))
            n.MarkRead();
        return Task.CompletedTask;
    }
}

// ── AuditLog Repository ───────────────────────────────────────────────
public class AuditLogRepository : InMemoryRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(InMemoryDataStore db) : base(db.AuditLogs, db) { }

    public Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<AuditLog>>(_store.Values
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderByDescending(al => al.Timestamp).ToList());

    public Task<string?> GetLastHashAsync(int tenantId, CancellationToken ct = default)
    {
        var last = _store.Values.Where(al => al.TenantId == tenantId).OrderByDescending(al => al.Id).FirstOrDefault();
        return Task.FromResult(last?.PrevHash);
    }
}

public class CostRepository : InMemoryRepository<CostEntry>, ICostRepository
{
    public CostRepository(InMemoryDataStore db) : base(db.CostEntries, db) { }

    public Task<IEnumerable<CostEntry>> GetByIssueAsync(int issueId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<CostEntry>>(_store.Values.Where(c => c.IssueId == issueId).ToList());

    public Task<IEnumerable<CostEntry>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<CostEntry>>(_store.Values.Where(c => c.Issue != null && c.Issue.ProjectId == projectId).ToList());
}

public class BudgetRepository : InMemoryRepository<Budget>, IBudgetRepository
{
    public BudgetRepository(InMemoryDataStore db) : base(db.Budgets, db) { }

    public Task<IEnumerable<Budget>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Budget>>(_store.Values.Where(b => b.ProjectId == projectId).ToList());
}

public class SlaRepository : InMemoryRepository<SlaPolicy>, ISlaRepository
{
    private readonly InMemoryDataStore _db;
    public SlaRepository(InMemoryDataStore db) : base(db.SlaPolicies, db) { _db = db; }

    public Task<IEnumerable<SlaPolicy>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<SlaPolicy>>(_store.Values.Where(s => s.ProjectId == projectId).ToList());

    public Task<IEnumerable<SlaBreach>> GetBreachesByIssueAsync(int issueId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<SlaBreach>>(_db.SlaBreaches.Values.Where(s => s.IssueId == issueId).ToList());
}

