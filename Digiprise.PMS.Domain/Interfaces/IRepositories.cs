using Digiprise.PMS.Domain.Entities;

namespace Digiprise.PMS.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByKeyAsync(int tenantId, string key, CancellationToken ct = default);
    Task<IEnumerable<Project>> GetByTenantAsync(int tenantId, CancellationToken ct = default);
    Task<bool> KeyExistsAsync(int tenantId, string key, CancellationToken ct = default);
}

public interface IIssueRepository : IRepository<Issue>
{
    Task<Issue?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Issue>> GetByProjectAsync(int projectId, CancellationToken ct = default);
    Task<IEnumerable<Issue>> GetBySprintAsync(int sprintId, CancellationToken ct = default);
    Task<IEnumerable<Issue>> GetBacklogAsync(int projectId, CancellationToken ct = default);
    Task<IEnumerable<Issue>> GetByAssigneeAsync(int userId, int tenantId, CancellationToken ct = default);
    Task<string> GenerateIssueKeyAsync(int projectId, string projectKey, CancellationToken ct = default);
    Task<IEnumerable<Issue>> SearchByIqlAsync(string iql, int tenantId, int currentUserId, int page, int pageSize, CancellationToken ct = default);
    Task<Dictionary<int, string>> GetJournalsAtBaselineAsync(IEnumerable<int> issueIds, DateTimeOffset baseline, CancellationToken ct = default);
}

public interface ISprintRepository : IRepository<Sprint>
{
    Task<Sprint?> GetActiveSprintAsync(int boardId, CancellationToken ct = default);
    Task<IEnumerable<Sprint>> GetByProjectAsync(int projectId, CancellationToken ct = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, int tenantId, CancellationToken ct = default);
    Task<IEnumerable<User>> GetByTenantAsync(int tenantId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, int tenantId, CancellationToken ct = default);
}

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);
}

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetUnreadAsync(int userId, CancellationToken ct = default);
    Task MarkAllReadAsync(int userId, CancellationToken ct = default);
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, int entityId, CancellationToken ct = default);
    Task<string?> GetLastHashAsync(int tenantId, CancellationToken ct = default);
}

public interface ICostRepository : IRepository<CostEntry>
{
    Task<IEnumerable<CostEntry>> GetByIssueAsync(int issueId, CancellationToken ct = default);
    Task<IEnumerable<CostEntry>> GetByProjectAsync(int projectId, CancellationToken ct = default);
}

public interface IBudgetRepository : IRepository<Budget>
{
    Task<IEnumerable<Budget>> GetByProjectAsync(int projectId, CancellationToken ct = default);
}

public interface ISlaRepository : IRepository<SlaPolicy>
{
    Task<IEnumerable<SlaPolicy>> GetByProjectAsync(int projectId, CancellationToken ct = default);
    Task<IEnumerable<SlaBreach>> GetBreachesByIssueAsync(int issueId, CancellationToken ct = default);
    Task<IEnumerable<SlaBreach>> GetActiveBreachesAsync(CancellationToken ct = default);
    Task AddBreachAsync(SlaBreach breach, CancellationToken ct = default);
    Task UpdateBreachAsync(SlaBreach breach, CancellationToken ct = default);
}

public interface IAutomationRepository : IRepository<AutomationRule>
{
    Task<IEnumerable<AutomationRule>> GetByProjectAsync(int projectId, CancellationToken ct = default);
    Task<IEnumerable<AutomationRule>> GetActiveRulesAsync(int tenantId, CancellationToken ct = default);
}
public interface IReportingRepository : IRepository<ReportDefinition>
{
    Task<IEnumerable<ReportDefinition>> GetByProjectAsync(int projectId, CancellationToken ct = default);
}
