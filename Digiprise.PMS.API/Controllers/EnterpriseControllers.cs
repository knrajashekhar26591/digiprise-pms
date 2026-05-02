using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

[Authorize]
[Route("api/v1/costs")]
public class CostsController : BaseController
{
    private readonly ICostService _costs;
    public CostsController(ICostService costs) => _costs = costs;

    [HttpGet("issue/{issueId:int}")]
    public async Task<IActionResult> GetByIssue(int issueId, CancellationToken ct)
        => Ok(await _costs.GetByIssueAsync(issueId, CurrentTenantId, ct));

    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetByProject(int projectId, CancellationToken ct)
        => Ok(await _costs.GetByProjectAsync(projectId, CurrentTenantId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCostEntryRequest request, CancellationToken ct)
    {
        var result = await _costs.CreateAsync(request, CurrentTenantId, CurrentUserId, ct);
        return Created($"/api/v1/costs/{result.Id}", result);
    }
}

[Authorize]
[Route("api/v1/budgets")]
public class BudgetsController : BaseController
{
    private readonly IBudgetService _budgets;
    public BudgetsController(IBudgetService budgets) => _budgets = budgets;

    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetByProject(int projectId, CancellationToken ct)
        => Ok(await _budgets.GetByProjectAsync(projectId, CurrentTenantId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request, CancellationToken ct)
    {
        var result = await _budgets.CreateAsync(request, CurrentTenantId, ct);
        return Created($"/api/v1/budgets/{result.Id}", result);
    }
}

[Authorize]
[Route("api/v1/sla")]
public class SlaController : BaseController
{
    private readonly ISlaService _sla;
    public SlaController(ISlaService sla) => _sla = sla;

    [HttpGet("project/{projectId:int}/policies")]
    public async Task<IActionResult> GetPolicies(int projectId, CancellationToken ct)
        => Ok(await _sla.GetByProjectAsync(projectId, CurrentTenantId, ct));

    [HttpGet("issue/{issueId:int}/breaches")]
    public async Task<IActionResult> GetBreaches(int issueId, CancellationToken ct)
        => Ok(await _sla.GetBreachesByIssueAsync(issueId, CurrentTenantId, ct));

    [HttpPost("policies")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateSlaPolicyRequest request, CancellationToken ct)
    {
        var result = await _sla.CreatePolicyAsync(request, CurrentTenantId, ct);
        return Created($"/api/v1/sla/policies/{result.Id}", result);
    }
}
