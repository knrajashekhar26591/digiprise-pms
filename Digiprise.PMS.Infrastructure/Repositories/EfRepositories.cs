using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Interfaces;
using Digiprise.PMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Digiprise.PMS.Infrastructure.Repositories;

public class EfRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly PmsDbContext _context;

    public EfRepository(PmsDbContext context)
    {
        _context = context;
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Set<T>().FindAsync(new object[] { id }, ct);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Set<T>().ToListAsync(ct);
    }

    public virtual Task AddAsync(T entity, CancellationToken ct = default)
    {
        _context.Set<T>().Add(entity);
        return Task.CompletedTask;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}

public class EfProjectRepository : EfRepository<Project>, IProjectRepository
{
    public EfProjectRepository(PmsDbContext context) : base(context) { }

    public async Task<Project?> GetByKeyAsync(int tenantId, string key, CancellationToken ct = default)
    {
        return await _context.Projects.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Key == key, ct);
    }

    public async Task<IEnumerable<Project>> GetByTenantAsync(int tenantId, CancellationToken ct = default)
    {
        return await _context.Projects.Where(p => p.TenantId == tenantId).ToListAsync(ct);
    }

    public async Task<bool> KeyExistsAsync(int tenantId, string key, CancellationToken ct = default)
    {
        return await _context.Projects.AnyAsync(p => p.TenantId == tenantId && p.Key == key, ct);
    }
}

public class EfIssueRepository : EfRepository<Issue>, IIssueRepository
{
    public EfIssueRepository(PmsDbContext context) : base(context) { }

    public async Task<Issue?> GetWithDetailsAsync(int id, CancellationToken ct = default)
    {
        return await _context.Issues
            .Include(i => i.Project)
            .Include(i => i.Assignee)
            .Include(i => i.Reporter)
            .Include(i => i.Sprint)
            .Include(i => i.Comments)
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<IEnumerable<Issue>> GetByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await _context.Issues.Where(i => i.ProjectId == projectId).ToListAsync(ct);
    }

    public async Task<IEnumerable<Issue>> GetBySprintAsync(int sprintId, CancellationToken ct = default)
    {
        return await _context.Issues.Where(i => i.SprintId == sprintId).ToListAsync(ct);
    }

    public async Task<IEnumerable<Issue>> GetBacklogAsync(int projectId, CancellationToken ct = default)
    {
        return await _context.Issues.Where(i => i.ProjectId == projectId && i.SprintId == null).ToListAsync(ct);
    }

    public async Task<IEnumerable<Issue>> GetByAssigneeAsync(int userId, int tenantId, CancellationToken ct = default)
    {
        return await _context.Issues.Where(i => i.TenantId == tenantId && i.AssigneeId == userId).ToListAsync(ct);
    }

    public async Task<string> GenerateIssueKeyAsync(int projectId, string projectKey, CancellationToken ct = default)
    {
        var count = await _context.Issues.CountAsync(i => i.ProjectId == projectId, ct);
        return $"{projectKey}-{count + 1}";
    }

    public async Task<IEnumerable<Issue>> SearchByIqlAsync(string iql, int tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        // Basic IQL mock fallback
        return await _context.Issues.Where(i => i.TenantId == tenantId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<Dictionary<int, string>> GetJournalsAtBaselineAsync(IEnumerable<int> issueIds, DateTimeOffset baseline, CancellationToken ct = default)
    {
        var result = new Dictionary<int, string>();
        foreach (var id in issueIds)
        {
            var journal = await _context.IssueJournals
                .Where(j => j.IssueId == id && j.CreatedAt <= baseline)
                .OrderByDescending(j => j.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (journal != null)
            {
                result[id] = journal.ChangedFields;
            }
        }
        return result;
    }
}

public class EfSprintRepository : EfRepository<Sprint>, ISprintRepository
{
    public EfSprintRepository(PmsDbContext context) : base(context) { }

    public async Task<Sprint?> GetActiveSprintAsync(int boardId, CancellationToken ct = default)
    {
        return await _context.Sprints.FirstOrDefaultAsync(s => s.BoardId == boardId && s.State == Digiprise.PMS.Domain.Enums.SprintState.Active, ct);
    }

    public async Task<IEnumerable<Sprint>> GetByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await _context.Sprints.Where(s => s.ProjectId == projectId).ToListAsync(ct);
    }
}

public class EfUserRepository : EfRepository<User>, IUserRepository
{
    public EfUserRepository(PmsDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, int tenantId, CancellationToken ct = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.TenantId == tenantId, ct);
    }

    public async Task<IEnumerable<User>> GetByTenantAsync(int tenantId, CancellationToken ct = default)
    {
        return await _context.Users.Where(u => u.TenantId == tenantId).ToListAsync(ct);
    }

    public async Task<bool> EmailExistsAsync(string email, int tenantId, CancellationToken ct = default)
    {
        return await _context.Users.AnyAsync(u => u.Email == email && u.TenantId == tenantId, ct);
    }
}

public class EfTenantRepository : EfRepository<Tenant>, ITenantRepository
{
    public EfTenantRepository(PmsDbContext context) : base(context) { }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        return await _context.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
    }
}

public class EfNotificationRepository : EfRepository<Notification>, INotificationRepository
{
    public EfNotificationRepository(PmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Notification>> GetUnreadAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
    }

    public async Task MarkAllReadAsync(int userId, CancellationToken ct = default)
    {
        var unread = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var notification in unread)
        {
            notification.MarkRead();
        }
    }
}

public class EfAuditLogRepository : EfRepository<AuditLog>, IAuditLogRepository
{
    public EfAuditLogRepository(PmsDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId, CancellationToken ct = default)
    {
        return await _context.AuditLogs.Where(a => a.EntityType == entityType && a.EntityId == entityId).ToListAsync(ct);
    }

    public async Task<string?> GetLastHashAsync(int tenantId, CancellationToken ct = default)
    {
        var last = await _context.AuditLogs.Where(a => a.TenantId == tenantId).OrderByDescending(a => a.Id).FirstOrDefaultAsync(ct);
        return last?.PrevHash;
    }
}

public class EfCostRepository : EfRepository<CostEntry>, ICostRepository
{
    public EfCostRepository(PmsDbContext context) : base(context) { }

    public async Task<IEnumerable<CostEntry>> GetByIssueAsync(int issueId, CancellationToken ct = default)
    {
        return await _context.CostEntries.Where(c => c.IssueId == issueId).ToListAsync(ct);
    }

    public async Task<IEnumerable<CostEntry>> GetByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await _context.CostEntries.Include(c => c.Issue).Where(c => c.Issue!.ProjectId == projectId).ToListAsync(ct);
    }
}

public class EfBudgetRepository : EfRepository<Budget>, IBudgetRepository
{
    public EfBudgetRepository(PmsDbContext context) : base(context) { }

    public async Task<IEnumerable<Budget>> GetByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await _context.Budgets.Where(b => b.ProjectId == projectId).ToListAsync(ct);
    }
}

public class EfSlaRepository : EfRepository<SlaPolicy>, ISlaRepository
{
    public EfSlaRepository(PmsDbContext context) : base(context) { }

    public async Task<IEnumerable<SlaPolicy>> GetByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await _context.SlaPolicies.Where(s => s.ProjectId == projectId).ToListAsync(ct);
    }

    public async Task<IEnumerable<SlaBreach>> GetBreachesByIssueAsync(int issueId, CancellationToken ct = default)
    {
        return await _context.SlaBreaches.Where(s => s.IssueId == issueId).ToListAsync(ct);
    }

    public async Task<IEnumerable<SlaBreach>> GetActiveBreachesAsync(CancellationToken ct = default)
    {
        return await _context.SlaBreaches.Include(s => s.Issue).Include(s => s.Policy).Where(s => s.State != "Breached").ToListAsync(ct);
    }

    public async Task AddBreachAsync(SlaBreach breach, CancellationToken ct = default)
    {
        _context.SlaBreaches.Add(breach);
        await Task.CompletedTask;
    }

    public async Task UpdateBreachAsync(SlaBreach breach, CancellationToken ct = default)
    {
        _context.SlaBreaches.Update(breach);
        await Task.CompletedTask;
    }
}

public class EfAutomationRepository : EfRepository<AutomationRule>, IAutomationRepository
{
    public EfAutomationRepository(PmsDbContext context) : base(context) { }

    public async Task<IEnumerable<AutomationRule>> GetByProjectAsync(int projectId, CancellationToken ct = default)
    {
        return await _context.AutomationRules.Where(r => r.ProjectId == projectId).ToListAsync(ct);
    }

    public async Task<IEnumerable<AutomationRule>> GetActiveRulesAsync(int tenantId, CancellationToken ct = default)
    {
        // AutomationRule doesn't have TenantId directly in SupportingEntities, but it has ProjectId.
        // Projects have TenantId.
        return await _context.AutomationRules
            .Include(r => r.Project)
            .Where(r => r.IsActive && r.Project.TenantId == tenantId)
            .ToListAsync(ct);
    }
}



