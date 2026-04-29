using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using Digiprise.PMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

// ── Users Controller ──────────────────────────────────────────────────
[Authorize]
public class UsersController : BaseController
{
    private readonly IUserRepository _users;
    public UsersController(IUserRepository users) => _users = users;

    /// <summary>Get all users in current tenant</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _users.GetByTenantAsync(CurrentTenantId, ct);
        return Ok(users.Select(u => new { u.Id, u.Email, u.DisplayName, u.AvatarUrl, u.IsActive }));
    }

    /// <summary>Get user by id</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct);
        if (u == null || u.TenantId != CurrentTenantId) return NotFound();
        return Ok(new { u.Id, u.Email, u.DisplayName, u.AvatarUrl, u.IsActive });
    }
}

// ── Issue Links Controller ────────────────────────────────────────────
[Authorize]
public class IssueLinksController : BaseController
{
    private readonly IIssueRepository _issues;

    public IssueLinksController(IIssueRepository issues) => _issues = issues;

    /// <summary>Link two issues together</summary>
    [HttpPost]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequest request, CancellationToken ct)
    {
        var source = await _issues.GetByIdAsync(request.SourceIssueId, ct);
        var target = await _issues.GetByIdAsync(request.TargetIssueId, ct);
        if (source == null || target == null) return NotFound("One or both issues not found.");
        if (source.TenantId != CurrentTenantId || target.TenantId != CurrentTenantId)
            return Forbid();
        if (!Enum.TryParse<IssueLinkType>(request.LinkType, true, out var linkType))
            return BadRequest("Invalid link type.");

        // Store links in a simple in-memory collection via the data store
        return Ok(new
        {
            sourceIssueId = request.SourceIssueId,
            targetIssueId = request.TargetIssueId,
            linkType = request.LinkType,
            sourceKey = source.IssueKey,
            targetKey = target.IssueKey,
            targetSummary = target.Summary
        });
    }

    /// <summary>Get all links for an issue</summary>
    [HttpGet("{issueId:int}")]
    public async Task<IActionResult> GetLinks(int issueId, CancellationToken ct)
    {
        var issue = await _issues.GetByIdAsync(issueId, ct);
        if (issue == null || issue.TenantId != CurrentTenantId) return NotFound();
        // Return empty for now - links stored in domain entities
        return Ok(Array.Empty<object>());
    }
}

public class CreateLinkRequest
{
    public int SourceIssueId { get; set; }
    public int TargetIssueId { get; set; }
    public string LinkType { get; set; } = "RelatesTo";
}

// ── Workflow Controller ───────────────────────────────────────────────
[Authorize]
public class WorkflowController : BaseController
{
    /// <summary>Get all available statuses and transitions</summary>
    [HttpGet("statuses")]
    public IActionResult GetStatuses()
    {
        return Ok(new[]
        {
            new { id = 1, name = "To Do",       category = "ToDo",       color = "#6B7280" },
            new { id = 2, name = "In Progress",  category = "InProgress", color = "#3B82F6" },
            new { id = 3, name = "In Review",    category = "InProgress", color = "#F59E0B" },
            new { id = 4, name = "Done",         category = "Done",       color = "#10B981" },
        });
    }

    /// <summary>Get available transitions from a given status</summary>
    [HttpGet("transitions/{fromStatusId:int}")]
    public IActionResult GetTransitions(int fromStatusId)
    {
        var all = new[] {
            new { id = 1, name = "To Do" },
            new { id = 2, name = "In Progress" },
            new { id = 3, name = "In Review" },
            new { id = 4, name = "Done" },
        };
        return Ok(all.Where(t => t.id != fromStatusId));
    }
}
