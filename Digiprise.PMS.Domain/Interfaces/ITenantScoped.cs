namespace Digiprise.PMS.Domain.Interfaces;

/// <summary>
/// Marks an entity as being scoped to a specific Tenant.
/// Used by EF Core Global Query Filters and SQL Server Row-Level Security.
/// </summary>
public interface ITenantScoped
{
    int TenantId { get; }
}
