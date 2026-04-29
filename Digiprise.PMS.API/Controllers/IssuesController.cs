using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

/// <summary>Issue management — full CRUD, transitions, comments, bulk ops, and IQL search</summary>
[Authorize]
public class IssuesController : BaseController
{
    private readonly IIssueService _issues;

    public IssuesController(IIssueService issues) => _issues = issues;

    /// <summary>Get a single issue with all details</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var issue = await _issues.GetByIdAsync(id, CurrentTenantId, ct);
        return issue == null ? NotFound() : Ok(issue);
    }

    /// <summary>List issues for a project</summary>
    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetByProject(int projectId, CancellationToken ct)
    {
        var issues = await _issues.GetByProjectAsync(projectId, CurrentTenantId, ct);
        return Ok(issues);
    }

    /// <summary>Get backlog for a project (issues not in any sprint)</summary>
    [HttpGet("project/{projectId:int}/backlog")]
    public async Task<IActionResult> GetBacklog(int projectId, CancellationToken ct)
    {
        var issues = await _issues.GetBacklogAsync(projectId, CurrentTenantId, ct);
        return Ok(issues);
    }

    /// <summary>Search issues using IQL (Internal Query Language)</summary>
    /// <remarks>
    /// IQL Examples:
    /// - `project = DIG AND status IN ("In Progress", "In Review")`
    /// - `assignee = currentUser() AND updated >= -7d`
    /// - `issuetype = Bug AND priority IN (Critical, Blocker)`
    /// </remarks>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] IssueSearchRequest request, CancellationToken ct)
    {
        var issues = await _issues.SearchAsync(request, CurrentTenantId, ct);
        return Ok(new { issues, total = issues.Count() });
    }

    /// <summary>Create a new issue</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIssueRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var issue = await _issues.CreateAsync(request, CurrentTenantId, CurrentUserId, ct);
            return CreatedAtAction(nameof(GetById), new { id = issue.Id }, issue);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Full update of an issue (use PATCH for partial)</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateIssueRequest request, CancellationToken ct)
    {
        try
        {
            var issue = await _issues.UpdateAsync(id, request, CurrentTenantId, CurrentUserId, ct);
            return Ok(issue);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Partial update (same as PUT in this implementation)</summary>
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Patch(int id, [FromBody] UpdateIssueRequest request, CancellationToken ct)
        => await Update(id, request, ct);

    /// <summary>Execute a workflow transition on an issue</summary>
    [HttpPost("{id:int}/transitions")]
    public async Task<IActionResult> Transition(int id, [FromBody] TransitionIssueRequest request, CancellationToken ct)
    {
        try
        {
            await _issues.TransitionAsync(id, request, CurrentTenantId, CurrentUserId, ct);
            return NoContent();
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Delete an issue</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _issues.DeleteAsync(id, CurrentTenantId, CurrentUserId, ct); return NoContent(); }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Bulk update up to 50 issues at once</summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> Bulk([FromBody] BulkIssueRequest request, CancellationToken ct)
    {
        try { await _issues.BulkUpdateAsync(request, CurrentTenantId, CurrentUserId, ct); return NoContent(); }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Add a comment to an issue</summary>
    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var comment = await _issues.AddCommentAsync(id, request, CurrentTenantId, CurrentUserId, ct);
            return Created($"/api/v1/issues/{id}/comments/{comment.Id}", comment);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Get all comments for an issue</summary>
    [HttpGet("{id:int}/comments")]
    public async Task<IActionResult> GetComments(int id, CancellationToken ct)
    {
        var comments = await _issues.GetCommentsAsync(id, CurrentTenantId, ct);
        return Ok(comments);
    }
}
