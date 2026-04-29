using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.DTOs;
using Digiprise.PMS.Contracts.Requests;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using Digiprise.PMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Digiprise.PMS.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository projects, IUserRepository users, ILogger<ProjectService> logger)
    {
        _projects = projects;
        _users = users;
        _logger = logger;
    }

    public async Task<ProjectDto?> GetByIdAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null || project.TenantId != tenantId) return null;
        return await MapAsync(project, ct);
    }

    public async Task<ProjectDto?> GetByKeyAsync(string key, int tenantId, CancellationToken ct = default)
    {
        var project = await _projects.GetByKeyAsync(tenantId, key.ToUpperInvariant(), ct);
        return project == null ? null : await MapAsync(project, ct);
    }

    public async Task<IEnumerable<ProjectDto>> GetAllAsync(int tenantId, CancellationToken ct = default)
    {
        var projects = await _projects.GetByTenantAsync(tenantId, ct);
        var result = new List<ProjectDto>();
        foreach (var p in projects)
            result.Add(await MapAsync(p, ct));
        return result;
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, int tenantId, int currentUserId, CancellationToken ct = default)
    {
        if (await _projects.KeyExistsAsync(tenantId, request.Key, ct))
            throw new InvalidOperationException($"Project key '{request.Key}' already exists in this tenant.");

        var boardType = Enum.TryParse<BoardType>(request.BoardType, out var bt) ? bt : BoardType.Scrum;
        var project = Project.Create(tenantId, request.Key, request.Name, currentUserId, boardType);
        project.Update(request.Name, request.Description, request.Icon, request.Color, null, request.StartDate, request.TargetDate);

        await _projects.AddAsync(project, ct);
        await _projects.SaveChangesAsync(ct);

        _logger.LogInformation("Project {Key} created by user {UserId} in tenant {TenantId}", project.Key, currentUserId, tenantId);
        return await MapAsync(project, ct);
    }

    public async Task<ProjectDto> UpdateAsync(int projectId, UpdateProjectRequest request, int tenantId, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(projectId, ct)
            ?? throw new KeyNotFoundException($"Project {projectId} not found.");
        if (project.TenantId != tenantId) throw new UnauthorizedAccessException();

        project.Update(request.Name, request.Description, request.Icon, request.Color, request.Category, request.StartDate, request.TargetDate);
        await _projects.UpdateAsync(project, ct);
        await _projects.SaveChangesAsync(ct);
        return await MapAsync(project, ct);
    }

    public async Task ArchiveAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(projectId, ct) ?? throw new KeyNotFoundException();
        if (project.TenantId != tenantId) throw new UnauthorizedAccessException();
        project.Archive();
        await _projects.UpdateAsync(project, ct);
        await _projects.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(projectId, ct) ?? throw new KeyNotFoundException();
        if (project.TenantId != tenantId) throw new UnauthorizedAccessException();
        project.Delete();
        await _projects.UpdateAsync(project, ct);
        await _projects.SaveChangesAsync(ct);
    }

    public async Task AddMemberAsync(int projectId, AddMemberRequest request, int tenantId, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(projectId, ct) ?? throw new KeyNotFoundException();
        if (project.TenantId != tenantId) throw new UnauthorizedAccessException();
        // Member management delegated to infrastructure repo
        await _projects.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(int projectId, int userId, int tenantId, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(projectId, ct) ?? throw new KeyNotFoundException();
        if (project.TenantId != tenantId) throw new UnauthorizedAccessException();
        await _projects.SaveChangesAsync(ct);
    }

    private async Task<ProjectDto> MapAsync(Project p, CancellationToken ct)
    {
        var lead = await _users.GetByIdAsync(p.LeadUserId, ct);
        return new ProjectDto(p.Id, p.Key, p.Name, p.Description, p.Icon, p.Color,
            p.BoardType.ToString(), p.Status.ToString(),
            p.LeadUserId, lead?.DisplayName, p.CreatedAt, p.TargetDate);
    }
}
