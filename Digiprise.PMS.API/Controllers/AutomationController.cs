using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

[Authorize]
[Route("api/v1/automation")]
public class AutomationController : BaseController
{
    private readonly IAutomationService _automation;
    public AutomationController(IAutomationService automation) => _automation = automation;

    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetByProject(int projectId, CancellationToken ct)
        => Ok(await _automation.GetByProjectAsync(projectId, CurrentTenantId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAutomationRuleRequest request, CancellationToken ct)
    {
        var result = await _automation.CreateRuleAsync(request.ProjectId, request.Name, request.TriggerConfig, request.ConditionConfig, request.ActionConfig, CurrentTenantId, ct);
        return Created($"/api/v1/automation/{result.Id}", result);
    }
}

public class CreateAutomationRuleRequest
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TriggerConfig { get; set; } = "{}";
    public string ConditionConfig { get; set; } = "[]";
    public string ActionConfig { get; set; } = "[]";
}
