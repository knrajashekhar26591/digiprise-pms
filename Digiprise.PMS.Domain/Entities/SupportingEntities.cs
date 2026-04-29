using Digiprise.PMS.Domain.Enums;

namespace Digiprise.PMS.Domain.Entities;

public class Sprint : BaseEntity
{
    public int BoardId { get; private set; }
    public int ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SprintState State { get; private set; } = SprintState.Created;
    public int? VelocityPoints { get; private set; }

    public ICollection<Issue> Issues { get; private set; } = new List<Issue>();

    protected Sprint() { }

    public static Sprint Create(int boardId, int projectId, string name, DateTime? startDate, DateTime? endDate, string? goal = null)
    {
        return new Sprint
        {
            BoardId = boardId,
            ProjectId = projectId,
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            Goal = goal,
            State = SprintState.Created
        };
    }

    public void Start()
    {
        if (State != SprintState.Created)
            throw new InvalidOperationException("Only Created sprints can be started.");
        State = SprintState.Active;
        Touch();
    }

    public void Close(int velocityPoints)
    {
        if (State != SprintState.Active)
            throw new InvalidOperationException("Only Active sprints can be closed.");
        State = SprintState.Closed;
        VelocityPoints = velocityPoints;
        Touch();
    }

    public void UpdateGoal(string? goal) { Goal = goal; Touch(); }
}

public class Comment : BaseEntity
{
    public int IssueId { get; private set; }
    public int UserId { get; private set; }
    public string Body { get; private set; } = string.Empty; // HTML
    public bool IsDeleted { get; private set; }
    public int? ParentCommentId { get; private set; }

    public User? Author { get; private set; }

    protected Comment() { }

    public static Comment Create(int issueId, int userId, string body, int? parentCommentId = null)
    {
        return new Comment { IssueId = issueId, UserId = userId, Body = body, ParentCommentId = parentCommentId };
    }

    public void Edit(string body) { Body = body; Touch(); }
    public void SoftDelete() { IsDeleted = true; Touch(); }
}

public class Attachment : BaseEntity
{
    public int IssueId { get; private set; }
    public int UserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;

    protected Attachment() { }

    public static Attachment Create(int issueId, int userId, string fileName, long fileSize, string contentType, string storagePath)
    {
        if (fileSize > 25 * 1024 * 1024)
            throw new InvalidOperationException("Attachment exceeds 25 MB limit.");

        return new Attachment
        {
            IssueId = issueId,
            UserId = userId,
            FileName = fileName,
            FileSize = fileSize,
            ContentType = contentType,
            StoragePath = storagePath
        };
    }
}

public class IssueLink : BaseEntity
{
    public int SourceIssueId { get; private set; }
    public int TargetIssueId { get; private set; }
    public IssueLinkType LinkType { get; private set; }
    public int CreatedByUserId { get; private set; }

    protected IssueLink() { }

    public static IssueLink Create(int sourceId, int targetId, IssueLinkType linkType, int byUserId)
    {
        if (sourceId == targetId)
            throw new InvalidOperationException("Cannot link an issue to itself.");
        return new IssueLink { SourceIssueId = sourceId, TargetIssueId = targetId, LinkType = linkType, CreatedByUserId = byUserId };
    }
}

public class IssueHistory : BaseEntity
{
    public int IssueId { get; private set; }
    public int UserId { get; private set; }
    public string ChangeType { get; private set; } = string.Empty;
    public string? FieldName { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTime ChangedAt { get; private set; } = DateTime.UtcNow;

    protected IssueHistory() { }

    public static IssueHistory Create(int issueId, int userId, string changeType, string? fieldName, string? oldValue, string? newValue)
    {
        return new IssueHistory
        {
            IssueId = issueId,
            UserId = userId,
            ChangeType = changeType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = DateTime.UtcNow
        };
    }
}

public class IssueCustomField : BaseEntity
{
    public int IssueId { get; private set; }
    public int FieldDefinitionId { get; private set; }
    public string? ValueText { get; private set; }
    public decimal? ValueNumber { get; private set; }
    public DateTimeOffset? ValueDate { get; private set; }
    public int? ValueUserId { get; private set; }
    public int? ValueOptionId { get; private set; }

    protected IssueCustomField() { }

    public static IssueCustomField CreateText(int issueId, int fieldId, string value)
        => new() { IssueId = issueId, FieldDefinitionId = fieldId, ValueText = value };

    public static IssueCustomField CreateNumber(int issueId, int fieldId, decimal value)
        => new() { IssueId = issueId, FieldDefinitionId = fieldId, ValueNumber = value };
}

public class WorkflowStatus : BaseEntity
{
    public int WorkflowId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public StatusCategory Category { get; private set; }
    public int Position { get; private set; }
    public string Color { get; private set; } = "#6B7280";

    protected WorkflowStatus() { }

    public static WorkflowStatus Create(int workflowId, string name, StatusCategory category, int position, string color = "#6B7280")
    {
        return new WorkflowStatus { WorkflowId = workflowId, Name = name, Category = category, Position = position, Color = color };
    }
}

public class ProjectMember : BaseEntity
{
    public int ProjectId { get; private set; }
    public int UserId { get; private set; }
    public string Role { get; private set; } = "Developer"; // ProjectAdmin, Developer, Reporter, Viewer, QA, ScrumMaster

    public User? User { get; private set; }

    protected ProjectMember() { }

    public static ProjectMember Create(int projectId, int userId, string role)
        => new() { ProjectId = projectId, UserId = userId, Role = role };

    public void ChangeRole(string role) { Role = role; Touch(); }
}

public class SprintSnapshot : BaseEntity
{
    public int SprintId { get; private set; }
    public DateTime SnapshotDate { get; private set; }
    public int TotalPoints { get; private set; }
    public int CompletedPoints { get; private set; }
    public int RemainingPoints { get; private set; }
    public int TotalIssues { get; private set; }
    public int CompletedIssues { get; private set; }

    protected SprintSnapshot() { }

    public static SprintSnapshot Create(int sprintId, int total, int completed, int remaining, int totalIssues, int completedIssues)
    {
        return new SprintSnapshot
        {
            SprintId = sprintId,
            SnapshotDate = DateTime.UtcNow.Date,
            TotalPoints = total,
            CompletedPoints = completed,
            RemainingPoints = remaining,
            TotalIssues = totalIssues,
            CompletedIssues = completedIssues
        };
    }
}

public class Notification : BaseEntity
{
    public int UserId { get; private set; }
    public int TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public int? EntityId { get; private set; }
    public bool IsRead { get; private set; }

    protected Notification() { }

    public static Notification Create(int userId, int tenantId, string title, string body, string? entityType = null, int? entityId = null)
        => new() { UserId = userId, TenantId = tenantId, Title = title, Body = body, EntityType = entityType, EntityId = entityId, IsRead = false };

    public void MarkRead() { IsRead = true; Touch(); }
}

public class AuditLog : BaseEntity
{
    public int TenantId { get; private set; }
    public int UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public int EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? OldValue { get; private set; } // JSON
    public string? NewValue { get; private set; } // JSON
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? PrevHash { get; private set; } // SHA-256 chain

    protected AuditLog() { }

    public static AuditLog Create(int tenantId, int userId, string entityType, int entityId, string action,
        string? oldValue, string? newValue, string? ip, string? ua, string? prevHash)
    {
        return new AuditLog
        {
            TenantId = tenantId, UserId = userId, EntityType = entityType, EntityId = entityId,
            Action = action, OldValue = oldValue, NewValue = newValue,
            IpAddress = ip, UserAgent = ua, Timestamp = DateTime.UtcNow, PrevHash = prevHash
        };
    }
}

public class AutomationRule : BaseEntity
{
    public int ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public string TriggerConfig { get; private set; } = "{}"; // JSON
    public string ConditionConfig { get; private set; } = "[]"; // JSON
    public string ActionConfig { get; private set; } = "[]"; // JSON
    public DateTime? LastRunAt { get; private set; }

    protected AutomationRule() { }

    public static AutomationRule Create(int projectId, string name, string triggerJson, string conditionJson, string actionJson)
        => new() { ProjectId = projectId, Name = name, TriggerConfig = triggerJson, ConditionConfig = conditionJson, ActionConfig = actionJson };

    public void Toggle() { IsActive = !IsActive; Touch(); }
    public void RecordRun() { LastRunAt = DateTime.UtcNow; Touch(); }
}
