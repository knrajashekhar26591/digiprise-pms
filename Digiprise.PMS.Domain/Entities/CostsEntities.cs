using Digiprise.PMS.Domain.Interfaces;

namespace Digiprise.PMS.Domain.Entities;

public class CostType : BaseEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    
    protected CostType() { }

    public static CostType Create(int tenantId, string name, string? description, bool isDefault = false)
        => new() { TenantId = tenantId, Name = name, Description = description, IsDefault = isDefault };
}

public class Budget : BaseEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal MaterialBudgetCents { get; private set; }
    public decimal LaborBudgetCents { get; private set; }
    public string? Description { get; private set; }

    public Project? Project { get; private set; }
    private readonly List<CostEntry> _costEntries = new();
    public IReadOnlyCollection<CostEntry> CostEntries => _costEntries.AsReadOnly();

    protected Budget() { }

    public static Budget Create(int tenantId, int projectId, string name, decimal materialCents, decimal laborCents, string? desc = null)
        => new() { TenantId = tenantId, ProjectId = projectId, Name = name, MaterialBudgetCents = materialCents, LaborBudgetCents = laborCents, Description = desc };
}

public class CostEntry : BaseEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int IssueId { get; private set; } // Replaces WorkPackageId for v1 compatibility
    public int UserId { get; private set; }
    public int CostTypeId { get; private set; }
    public DateTime SpentOn { get; private set; }
    public decimal Units { get; private set; }
    public long UnitCostCents { get; private set; }
    public string? Comment { get; private set; }
    public int? BudgetId { get; private set; }

    public Issue? Issue { get; private set; }
    public User? User { get; private set; }
    public CostType? CostType { get; private set; }
    public Budget? Budget { get; private set; }

    protected CostEntry() { }

    public static CostEntry Create(int tenantId, int issueId, int userId, int costTypeId, DateTime spentOn, decimal units, long unitCostCents, string? comment = null, int? budgetId = null)
    {
        return new CostEntry
        {
            TenantId = tenantId,
            IssueId = issueId,
            UserId = userId,
            CostTypeId = costTypeId,
            SpentOn = spentOn.Date,
            Units = units,
            UnitCostCents = unitCostCents,
            Comment = comment,
            BudgetId = budgetId
        };
    }
}
