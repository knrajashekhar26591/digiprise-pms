using Digiprise.PMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

[Authorize]
[Route("api/v1/reports")]
public class ReportingController : BaseController
{
    private readonly IReportingService _reporting;
    public ReportingController(IReportingService reporting) => _reporting = reporting;

    [HttpGet("project/{projectId:int}/velocity")]
    public async Task<IActionResult> GetVelocity(int projectId, CancellationToken ct)
        => Ok(await _reporting.GetVelocityAsync(projectId, CurrentTenantId, ct));

    [HttpGet("project/{projectId:int}/cycle-time")]
    public async Task<IActionResult> GetCycleTime(int projectId, CancellationToken ct)
        => Ok(await _reporting.GetCycleTimeAsync(projectId, CurrentTenantId, ct));

    [HttpGet("project/{projectId:int}/sla-performance")]
    public async Task<IActionResult> GetSlaPerformance(int projectId, CancellationToken ct)
        => Ok(await _reporting.GetSlaPerformanceAsync(projectId, CurrentTenantId, ct));
}
