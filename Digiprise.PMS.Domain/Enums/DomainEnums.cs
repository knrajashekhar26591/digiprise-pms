namespace Digiprise.PMS.Domain.Enums;

public enum IssueType { Epic, Story, Subtask, SubSubtask, Bug, Task }
public enum Priority { Trivial, Minor, Major, Critical, Blocker }
public enum StatusCategory { ToDo, InProgress, Done }
public enum BoardType { Scrum, Kanban, Hybrid }
public enum ProjectStatus { Active, Archived, Deleted }
public enum SprintState { Created, Active, Closed }
public enum IssueLinkType { Blocks, IsBlockedBy, RelatesTo, Duplicates, IsDuplicatedBy, Clones, IsCLonedBy, CausedBy, IsCausing }
public enum NotificationChannel { InApp, Email }
public enum SlaState { WithinGoal, BreachingNow, Breached }
public enum AutomationTriggerType
{
    IssueCreated, IssueUpdated, IssueDeleted,
    StatusTransitioned, CommentAdded, AttachmentAdded,
    SprintStarted, SprintClosed,
    SLABreached, SLAWarning,
    ScheduledTime, WebhookReceived
}
