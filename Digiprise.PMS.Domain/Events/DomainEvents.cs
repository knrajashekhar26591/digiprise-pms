namespace Digiprise.PMS.Domain.Events;

public abstract class DomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public class IssueCreatedEvent : DomainEvent
{
    public int IssueId { get; }
    public int ProjectId { get; }
    public int TenantId { get; }
    public IssueCreatedEvent(int issueId, int projectId, int tenantId)
    { IssueId = issueId; ProjectId = projectId; TenantId = tenantId; }
}

public class IssueStatusChangedEvent : DomainEvent
{
    public int IssueId { get; }
    public int TenantId { get; }
    public int OldStatusId { get; }
    public int NewStatusId { get; }
    public int ChangedByUserId { get; }
    public IssueStatusChangedEvent(int issueId, int tenantId, int oldStatusId, int newStatusId, int changedBy)
    { IssueId = issueId; TenantId = tenantId; OldStatusId = oldStatusId; NewStatusId = newStatusId; ChangedByUserId = changedBy; }
}

public class CommentAddedEvent : DomainEvent
{
    public int IssueId { get; }
    public int CommentId { get; }
    public int AuthorId { get; }
    public CommentAddedEvent(int issueId, int commentId, int authorId)
    { IssueId = issueId; CommentId = commentId; AuthorId = authorId; }
}

public class SprintStartedEvent : DomainEvent
{
    public int SprintId { get; }
    public int ProjectId { get; }
    public SprintStartedEvent(int sprintId, int projectId) { SprintId = sprintId; ProjectId = projectId; }
}

public class SprintClosedEvent : DomainEvent
{
    public int SprintId { get; }
    public int ProjectId { get; }
    public SprintClosedEvent(int sprintId, int projectId) { SprintId = sprintId; ProjectId = projectId; }
}
