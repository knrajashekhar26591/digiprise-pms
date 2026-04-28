using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

/// <summary>Project management — CRUD, membership, and archival</summary>
[Authorize]
public class ProjectsController : BaseController
{
    private readonly IProjectService _projects;
    private readonly ISprintService _sprints;

    public ProjectsController(IProjectService projects, ISprintService sprints)
    { _projects = projects; _sprints = sprints; }

    /// <summary>List all projects for the current tenant</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var projects = await _projects.GetAllAsync(CurrentTenantId, ct);
        return Ok(projects);
    }

    /// <summary>Get project by ID</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(id, CurrentTenantId, ct);
        return project == null ? NotFound() : Ok(project);
    }

    /// <summary>Get project by key (e.g., DIG)</summary>
    [HttpGet("key/{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
    {
        var project = await _projects.GetByKeyAsync(key, CurrentTenantId, ct);
        return project == null ? NotFound() : Ok(project);
    }

    /// <summary>Create a new project</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var project = await _projects.CreateAsync(request, CurrentTenantId, CurrentUserId, ct);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Update project details</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var project = await _projects.UpdateAsync(id, request, CurrentTenantId, ct);
            return Ok(project);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Archive a project</summary>
    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id, CancellationToken ct)
    {
        try { await _projects.ArchiveAsync(id, CurrentTenantId, ct); return NoContent(); }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Soft-delete a project</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _projects.DeleteAsync(id, CurrentTenantId, ct); return NoContent(); }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Add a member to project</summary>
    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberRequest request, CancellationToken ct)
    {
        try { await _projects.AddMemberAsync(id, request, CurrentTenantId, ct); return NoContent(); }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Remove a member from project</summary>
    [HttpDelete("{id:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int userId, CancellationToken ct)
    {
        try { await _projects.RemoveMemberAsync(id, userId, CurrentTenantId, ct); return NoContent(); }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>Get all sprints for a project</summary>
    [HttpGet("{id:int}/sprints")]
    public async Task<IActionResult> GetSprints(int id, CancellationToken ct)
    {
        var sprints = await _sprints.GetByProjectAsync(id, ct);
        return Ok(sprints);
    }

    /// <summary>Create a sprint for a project</summary>
    [HttpPost("{id:int}/sprints")]
    public async Task<IActionResult> CreateSprint(int id, [FromBody] CreateSprintRequest request, CancellationToken ct)
    {
        try
        {
            var sprint = await _sprints.CreateAsync(id, request, ct);
            return CreatedAtAction(nameof(GetSprints), new { id }, sprint);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}
