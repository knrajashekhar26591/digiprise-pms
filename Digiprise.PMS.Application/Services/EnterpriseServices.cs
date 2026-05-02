using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.DTOs;
using Digiprise.PMS.Contracts.Requests;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using Digiprise.PMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Digiprise.PMS.Application.Services;

public class CostService : ICostService
{
    private readonly ICostRepository _costs;
    private readonly IIssueRepository _issues;
    private readonly IUserRepository _users;
    private readonly ILogger<CostService> _logger;

    public CostService(ICostRepository costs, IIssueRepository issues, IUserRepository users, ILogger<CostService> logger)
    {
        _costs = costs;
        _issues = issues;
        _users = users;
        _logger = logger;
    }

    public async Task<IEnumerable<CostEntryDto>> GetByIssueAsync(int issueId, int tenantId, CancellationToken ct = default)
    {
        var entries = await _costs.GetByIssueAsync(issueId, ct);
        return entries.Where(e => e.TenantId == tenantId).Select(e => Map(e));
    }

    public async Task<IEnumerable<CostEntryDto>> GetByProjectAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var entries = await _costs.GetByProjectAsync(projectId, ct);
        return entries.Where(e => e.TenantId == tenantId).Select(e => Map(e));
    }

    public async Task<CostEntryDto> CreateAsync(CreateCostEntryRequest request, int tenantId, int currentUserId, CancellationToken ct = default)
    {
        var entry = CostEntry.Create(tenantId, request.IssueId, currentUserId, request.CostTypeId, request.SpentOn, request.Units, request.UnitCostCents, request.Comment, request.BudgetId);
        await _costs.AddAsync(entry, ct);
        await _costs.SaveChangesAsync(ct);
        return Map(entry);
    }

    private static CostEntryDto Map(CostEntry e) => new CostEntryDto(
        e.Id, e.IssueId, e.Issue?.IssueKey ?? "", e.UserId, e.User?.DisplayName ?? "Unknown", 
        e.CostTypeId, e.CostType?.Name ?? "General", e.SpentOn, e.Units, e.UnitCostCents, e.Comment);
}

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgets;

    public BudgetService(IBudgetRepository budgets)
    {
        _budgets = budgets;
    }

    public async Task<IEnumerable<BudgetDto>> GetByProjectAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var budgets = await _budgets.GetByProjectAsync(projectId, ct);
        return budgets.Where(b => b.TenantId == tenantId).Select(b => Map(b));
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequest request, int tenantId, CancellationToken ct = default)
    {
        var budget = Budget.Create(tenantId, request.ProjectId, request.Name, request.MaterialBudgetCents, request.LaborBudgetCents, request.Description);
        await _budgets.AddAsync(budget, ct);
        await _budgets.SaveChangesAsync(ct);
        return Map(budget);
    }

    private static BudgetDto Map(Budget b) => new BudgetDto(b.Id, b.ProjectId, b.Name, b.MaterialBudgetCents, b.LaborBudgetCents, b.Description);
}

public class SlaService : ISlaService
{
    private readonly ISlaRepository _sla;

    public SlaService(ISlaRepository sla)
    {
        _sla = sla;
    }

    public async Task<IEnumerable<SlaPolicyDto>> GetByProjectAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var policies = await _sla.GetByProjectAsync(projectId, ct);
        return policies.Where(p => p.TenantId == tenantId).Select(p => Map(p));
    }

    public async Task<IEnumerable<SlaBreachDto>> GetBreachesByIssueAsync(int issueId, int tenantId, CancellationToken ct = default)
    {
        var breaches = await _sla.GetBreachesByIssueAsync(issueId, ct);
        // SlaBreach doesn't have TenantId directly, but linked via Issue/Policy
        return breaches.Select(b => MapBreach(b));
    }

    public async Task<SlaPolicyDto> CreatePolicyAsync(CreateSlaPolicyRequest request, int tenantId, CancellationToken ct = default)
    {
        Priority? priority = null;
        if (!string.IsNullOrEmpty(request.TargetPriority) && Enum.TryParse<Priority>(request.TargetPriority, true, out var p))
            priority = p;

        var policy = SlaPolicy.Create(tenantId, request.ProjectId, request.Name, priority, request.ResponseSecs, request.ResolutionSecs);
        await _sla.AddAsync(policy, ct);
        await _sla.SaveChangesAsync(ct);
        return Map(policy);
    }

    private static SlaPolicyDto Map(SlaPolicy p) => new SlaPolicyDto(p.Id, p.ProjectId, p.Name, p.TargetPriority?.ToString(), p.ResponseSecs, p.ResolutionSecs);
    private static SlaBreachDto MapBreach(SlaBreach b) => new SlaBreachDto(b.Id, b.IssueId, b.PolicyId, b.Type, b.State, b.StartedAt, b.BreachAt, b.BreachedAt);
}
