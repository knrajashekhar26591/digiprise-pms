using System.Text.Json;
using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.DTOs;
using Digiprise.PMS.Contracts.Requests;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using Digiprise.PMS.Domain.Events;
using Digiprise.PMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Digiprise.PMS.Application.Services;

public class IssueService : IIssueService
{
    private readonly IIssueRepository _issues;
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly ISprintRepository _sprints;
    private readonly IEventBus _eventBus;
    private readonly INotificationService _notifications;
    private readonly ILogger<IssueService> _logger;

    public IssueService(IIssueRepository issues, IProjectRepository projects,
        IUserRepository users, ISprintRepository sprints, IEventBus eventBus,
        INotificationService notifications, ILogger<IssueService> logger)
    {
        _issues = issues;
        _projects = projects;
        _users = users;
        _sprints = sprints;
        _eventBus = eventBus;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<IssueDto?> GetByIdAsync(int issueId, int tenantId, CancellationToken ct = default)
    {
        var issue = await _issues.GetWithDetailsAsync(issueId, ct);
        if (issue == null || issue.TenantId != tenantId) return null;
        return await MapFullAsync(issue, ct);
    }

    public async Task<IEnumerable<IssueListItemDto>> GetByProjectAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var issues = await _issues.GetByProjectAsync(projectId, ct);
        var result = new List<IssueListItemDto>();
        foreach (var i in issues.Where(x => x.TenantId == tenantId))
            result.Add(await MapListAsync(i, ct));
        return result;
    }

    public async Task<IEnumerable<IssueListItemDto>> GetBacklogAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var issues = await _issues.GetBacklogAsync(projectId, ct);
        var result = new List<IssueListItemDto>();
        foreach (var i in issues.Where(x => x.TenantId == tenantId))
            result.Add(await MapListAsync(i, ct));
        return result;
    }

    public async Task<IEnumerable<IssueListItemDto>> SearchAsync(IssueSearchRequest request, int tenantId, CancellationToken ct = default)
    {
        var issues = await _issues.SearchByIqlAsync(request.Iql ?? "", tenantId, request.StartAt / request.MaxResults, request.MaxResults, ct);
        var result = new List<IssueListItemDto>();
        foreach (var i in issues)
            result.Add(await MapListAsync(i, ct));
        return result;
    }

    public async Task<IssueDto> CreateAsync(CreateIssueRequest request, int tenantId, int currentUserId, CancellationToken ct = default)
    {
        var project = await _projects.GetByKeyAsync(tenantId, request.ProjectKey, ct)
            ?? throw new KeyNotFoundException($"Project '{request.ProjectKey}' not found.");

        var issueKey = await _issues.GenerateIssueKeyAsync(project.Id, project.Key, ct);

        if (!Enum.TryParse<IssueType>(request.IssueType, true, out var issueType))
            throw new ArgumentException($"Invalid issue type: {request.IssueType}");

        if (!Enum.TryParse<Priority>(request.Priority, true, out var priority))
            priority = Priority.Major;

        var issue = Issue.Create(tenantId, project.Id, issueKey, issueType, request.Summary, currentUserId, 1 /* default status */, priority);

        if (!string.IsNullOrEmpty(request.Description))
            issue.UpdateDescription(request.Description, currentUserId);
        if (request.AssigneeId.HasValue)
            issue.Assign(request.AssigneeId, currentUserId);
        if (request.StoryPoints.HasValue)
            issue.SetStoryPoints(request.StoryPoints, currentUserId);
        if (request.SprintId.HasValue)
            issue.AssignToSprint(request.SprintId, currentUserId);
        if (request.ParentIssueId.HasValue)
            issue.SetParent(request.ParentIssueId);
        if (request.Labels?.Any() == true)
            issue.SetLabels(JsonSerializer.Serialize(request.Labels));
        if (request.DueDate.HasValue)
            issue.SetDueDate(request.DueDate, currentUserId);

        await _issues.AddAsync(issue, ct);
        await _issues.SaveChangesAsync(ct);

        await _eventBus.PublishAsync(new IssueCreatedEvent(issue.Id, project.Id, tenantId), ct);

        if (request.AssigneeId.HasValue && request.AssigneeId.Value != currentUserId)
        {
            await _notifications.SendAsync(request.AssigneeId.Value, tenantId,
                $"Assigned: {issueKey}", request.Summary, "Issue", issue.Id, ct);
        }

        _logger.LogInformation("Issue {Key} created in project {ProjectKey}", issueKey, project.Key);
        return (await MapFullAsync(issue, ct))!;
    }

    public async Task<IssueDto> UpdateAsync(int issueId, UpdateIssueRequest request, int tenantId, int currentUserId, CancellationToken ct = default)
    {
        var issue = await _issues.GetByIdAsync(issueId, ct) ?? throw new KeyNotFoundException();
        if (issue.TenantId != tenantId) throw new UnauthorizedAccessException();

        if (request.Summary != null) issue.UpdateSummary(request.Summary, currentUserId);
        if (request.Description != null) issue.UpdateDescription(request.Description, currentUserId);
        if (request.AssigneeId != issue.AssigneeId) issue.Assign(request.AssigneeId, currentUserId);
        if (request.Priority != null && Enum.TryParse<Priority>(request.Priority, true, out var p))
            issue.ChangePriority(p, currentUserId);
        if (request.StoryPoints != null) issue.SetStoryPoints(request.StoryPoints, currentUserId);
        if (request.SprintId != null) issue.AssignToSprint(request.SprintId, currentUserId);
        if (request.Labels != null) issue.SetLabels(JsonSerializer.Serialize(request.Labels));
        if (request.DueDate != null) issue.SetDueDate(request.DueDate, currentUserId);

        await _issues.UpdateAsync(issue, ct);
        await _issues.SaveChangesAsync(ct);
        return (await MapFullAsync(issue, ct))!;
    }

    public async Task TransitionAsync(int issueId, TransitionIssueRequest request, int tenantId, int currentUserId, CancellationToken ct = default)
    {
        var issue = await _issues.GetByIdAsync(issueId, ct) ?? throw new KeyNotFoundException();
        if (issue.TenantId != tenantId) throw new UnauthorizedAccessException();

        var oldStatusId = issue.StatusId;
        // In a real implementation, validate transition via workflow engine
        issue.TransitionStatus(request.TransitionId, $"Status_{oldStatusId}", $"Status_{request.TransitionId}", currentUserId);

        if (!string.IsNullOrEmpty(request.Comment))
        {
            var comment = Comment.Create(issueId, currentUserId, request.Comment);
            // Would add comment here
        }

        await _issues.UpdateAsync(issue, ct);
        await _issues.SaveChangesAsync(ct);
        await _eventBus.PublishAsync(new IssueStatusChangedEvent(issueId, tenantId, oldStatusId, request.TransitionId, currentUserId), ct);
    }

    public async Task DeleteAsync(int issueId, int tenantId, int currentUserId, CancellationToken ct = default)
    {
        var issue = await _issues.GetByIdAsync(issueId, ct) ?? throw new KeyNotFoundException();
        if (issue.TenantId != tenantId) throw new UnauthorizedAccessException();
        await _issues.DeleteAsync(issue, ct);
        await _issues.SaveChangesAsync(ct);
        _logger.LogInformation("Issue {Id} deleted by user {UserId}", issueId, currentUserId);
    }

    public async Task BulkUpdateAsync(BulkIssueRequest request, int tenantId, int currentUserId, CancellationToken ct = default)
    {
        if (request.IssueIds.Length > 50)
            throw new InvalidOperationException("Bulk operations limited to 50 issues.");

        foreach (var issueId in request.IssueIds)
        {
            var issue = await _issues.GetByIdAsync(issueId, ct);
            if (issue == null || issue.TenantId != tenantId) continue;

            switch (request.Action?.ToLower())
            {
                case "assign" when int.TryParse(request.Value, out var uid):
                    issue.Assign(uid, currentUserId); break;
                case "setpriority" when Enum.TryParse<Priority>(request.Value, true, out var pri):
                    issue.ChangePriority(pri, currentUserId); break;
                case "movetosprint" when int.TryParse(request.Value, out var sid):
                    issue.AssignToSprint(sid, currentUserId); break;
            }
            await _issues.UpdateAsync(issue, ct);
        }
        await _issues.SaveChangesAsync(ct);
    }

    public async Task<CommentDto> AddCommentAsync(int issueId, CreateCommentRequest request, int tenantId, int currentUserId, CancellationToken ct = default)
    {
        var issue = await _issues.GetByIdAsync(issueId, ct) ?? throw new KeyNotFoundException();
        if (issue.TenantId != tenantId) throw new UnauthorizedAccessException();

        var user = await _users.GetByIdAsync(currentUserId, ct);
        var now = DateTime.UtcNow;
        // In full implementation, comment would be persisted via repository
        return new CommentDto(0, issueId, currentUserId, user?.DisplayName ?? "Unknown", user?.AvatarUrl, request.Body, now, now);
    }

    public Task<IEnumerable<CommentDto>> GetCommentsAsync(int issueId, int tenantId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<CommentDto>>(Array.Empty<CommentDto>());

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "To Do" }, { 2, "In Progress" }, { 3, "In Review" }, { 4, "Done" }
    };
    private static string GetStatusName(int id) => StatusNames.TryGetValue(id, out var n) ? n : "To Do";

    private async Task<IssueDto?> MapFullAsync(Issue i, CancellationToken ct)
    {
        var assignee = i.AssigneeId.HasValue ? await _users.GetByIdAsync(i.AssigneeId.Value, ct) : null;
        var reporter = await _users.GetByIdAsync(i.ReporterId, ct);
        var project = await _projects.GetByIdAsync(i.ProjectId, ct);
        Sprint? sprint = null;
        if (i.SprintId.HasValue) sprint = await _sprints.GetByIdAsync(i.SprintId.Value, ct);

        return new IssueDto(
            i.Id, i.IssueKey, i.IssueType.ToString(), i.Summary, i.Description,
            i.ProjectId, project?.Key ?? "",
            i.StatusId, GetStatusName(i.StatusId), i.StatusId >= 4 ? "Done" : i.StatusId >= 2 ? "InProgress" : "ToDo",
            i.Priority.ToString(), i.AssigneeId, assignee?.DisplayName,
            i.ReporterId, reporter?.DisplayName ?? "Unknown",
            i.ParentIssueId, i.SprintId, sprint?.Name, i.StoryPoints, i.DueDate,
            i.Labels != null ? JsonSerializer.Deserialize<string[]>(i.Labels) : null,
            i.CreatedAt, i.UpdatedAt);
    }

    private async Task<IssueListItemDto> MapListAsync(Issue i, CancellationToken ct)
    {
        var assignee = i.AssigneeId.HasValue ? await _users.GetByIdAsync(i.AssigneeId.Value, ct) : null;
        return new IssueListItemDto(
            i.Id, i.IssueKey, i.IssueType.ToString(), i.Summary,
            i.Priority.ToString(), GetStatusName(i.StatusId), i.StatusId >= 4 ? "Done" : i.StatusId >= 2 ? "InProgress" : "ToDo",
            i.AssigneeId, assignee?.DisplayName, i.StoryPoints, i.UpdatedAt);
    }
}
