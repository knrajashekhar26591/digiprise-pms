using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

// ── Sprints Controller ────────────────────────────────────────────────
/// <summary>Sprint lifecycle management — start, close, burndown</summary>
[Authorize]
public class SprintsController : BaseController
{
    private readonly ISprintService _sprints;

    public SprintsController(ISprintService sprints) => _sprints = sprints;

    /// <summary>Get sprint by ID</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var sprint = await _sprints.GetByIdAsync(id, ct);
        return sprint == null ? NotFound() : Ok(sprint);
    }

    /// <summary>Get active sprint for a board</summary>
    [HttpGet("active/{boardId:int}")]
    public async Task<IActionResult> GetActive(int boardId, CancellationToken ct)
    {
        var sprint = await _sprints.GetActiveSprintAsync(boardId, ct);
        return sprint == null ? NotFound() : Ok(sprint);
    }

    /// <summary>Start a sprint</summary>
    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id, [FromBody] StartSprintRequest request, CancellationToken ct)
    {
        try
        {
            var sprint = await _sprints.StartAsync(id, request, CurrentUserId, ct);
            return Ok(sprint);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Close a sprint and record velocity</summary>
    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        try
        {
            var sprint = await _sprints.CloseAsync(id, CurrentUserId, ct);
            return Ok(sprint);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Get burndown chart data for a sprint</summary>
    [HttpGet("{id:int}/burndown")]
    public async Task<IActionResult> Burndown(int id, CancellationToken ct)
    {
        var points = await _sprints.GetBurndownAsync(id, ct);
        return Ok(points);
    }
}

// ── Dashboard Controller ──────────────────────────────────────────────
/// <summary>Dashboard widgets and summary data</summary>
[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>Get high-level dashboard summary for the current user</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var summary = await _dashboard.GetSummaryAsync(CurrentTenantId, CurrentUserId, ct);
        return Ok(summary);
    }

    /// <summary>Get issues assigned to the current user</summary>
    [HttpGet("assigned-to-me")]
    public async Task<IActionResult> AssignedToMe(CancellationToken ct)
    {
        var issues = await _dashboard.GetAssignedToMeAsync(CurrentUserId, CurrentTenantId, ct);
        return Ok(issues);
    }
}

// ── Notifications Controller ──────────────────────────────────────────
/// <summary>In-app notifications for the current user</summary>
[Authorize]
public class NotificationsController : BaseController
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    /// <summary>Get all unread notifications</summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread(CancellationToken ct)
    {
        var notes = await _notifications.GetUnreadAsync(CurrentUserId, ct);
        return Ok(notes);
    }

    /// <summary>Mark a single notification as read</summary>
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        try { await _notifications.MarkReadAsync(id, CurrentUserId, ct); return NoContent(); }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Mark all notifications as read</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifications.MarkAllReadAsync(CurrentUserId, ct);
        return NoContent();
    }
}

// ── Health Controller ─────────────────────────────────────────────────
/// <summary>Health check endpoints for load balancers and orchestrators</summary>
[Route("health")]
[ApiController]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    /// <summary>Liveness probe — is the process alive?</summary>
    [HttpGet("live")]
    public IActionResult Live() => Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });

    /// <summary>Readiness probe — is the app ready to serve traffic?</summary>
    [HttpGet("ready")]
    public IActionResult Ready() => Ok(new { status = "Ready", timestamp = DateTime.UtcNow, version = "1.0.0" });
}
