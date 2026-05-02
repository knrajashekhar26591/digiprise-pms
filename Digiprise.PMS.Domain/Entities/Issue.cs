using Digiprise.PMS.Domain.Enums;

namespace Digiprise.PMS.Domain.Entities;

public class Issue : BaseEntity
{
    public int TenantId { get; private set; }
    public int ProjectId { get; private set; }
    public string IssueKey { get; private set; } = string.Empty; // e.g. DIG-42
    public IssueType IssueType { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? Description { get; private set; } // HTML
    public int StatusId { get; private set; }
    public Priority Priority { get; private set; } = Priority.Major;
    public int? AssigneeId { get; private set; }
    public int ReporterId { get; private set; }
    public int? ParentIssueId { get; private set; }
    public int? SprintId { get; private set; }
    public int? EpicId { get; private set; }
    public int? StoryPoints { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? ResolutionDate { get; private set; }
    public int? OriginalEstimateMinutes { get; private set; }
    public int? TimeSpentMinutes { get; private set; }
    public string? Labels { get; private set; } // JSON array
    public int? FixVersionId { get; private set; }

    private readonly List<Comment> _comments = new();
    private readonly List<Attachment> _attachments = new();
    private readonly List<IssueLink> _issueLinks = new();
    private readonly List<IssueHistory> _history = new();
    private readonly List<IssueCustomField> _customFields = new();

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();

    public void AddComment(Comment comment)
    {
        _comments.Add(comment);
    }
    public IReadOnlyCollection<IssueLink> IssueLinks => _issueLinks.AsReadOnly();
    public IReadOnlyCollection<IssueHistory> History => _history.AsReadOnly();
    public IReadOnlyCollection<IssueCustomField> CustomFields => _customFields.AsReadOnly();

    public Project? Project { get; private set; }
    public WorkflowStatus? Status { get; private set; }
    public User? Assignee { get; private set; }
    public User? Reporter { get; private set; }
    public Sprint? Sprint { get; private set; }

    protected Issue() { }

    public static Issue Create(
        int tenantId, int projectId, string issueKey, IssueType issueType,
        string summary, int reporterId, int defaultStatusId, Priority priority = Priority.Major)
    {
        if (string.IsNullOrWhiteSpace(summary) || summary.Length > 255)
            throw new ArgumentException("Summary must be 1-255 characters.");

        return new Issue
        {
            TenantId = tenantId,
            ProjectId = projectId,
            IssueKey = issueKey,
            IssueType = issueType,
            Summary = summary,
            ReporterId = reporterId,
            StatusId = defaultStatusId,
            Priority = priority
        };
    }

    public void UpdateSummary(string summary, int byUserId)
    {
        RecordHistory("Summary", Summary, summary, byUserId);
        Summary = summary;
        Touch();
    }

    public void UpdateDescription(string? description, int byUserId)
    {
        RecordHistory("Description", Description ?? "", description ?? "", byUserId);
        Description = description;
        Touch();
    }

    public void Assign(int? assigneeId, int byUserId)
    {
        RecordHistory("Assignee", AssigneeId?.ToString() ?? "Unassigned", assigneeId?.ToString() ?? "Unassigned", byUserId);
        AssigneeId = assigneeId;
        Touch();
    }

    public void ChangePriority(Priority priority, int byUserId)
    {
        RecordHistory("Priority", Priority.ToString(), priority.ToString(), byUserId);
        Priority = priority;
        Touch();
    }

    public void TransitionStatus(int newStatusId, string fromStatusName, string toStatusName, int byUserId)
    {
        RecordHistory("Status", fromStatusName, toStatusName, byUserId);
        StatusId = newStatusId;
        Touch();
    }

    public void SetStoryPoints(int? points, int byUserId)
    {
        RecordHistory("StoryPoints", StoryPoints?.ToString() ?? "null", points?.ToString() ?? "null", byUserId);
        StoryPoints = points;
        Touch();
    }

    public void AssignToSprint(int? sprintId, int byUserId)
    {
        RecordHistory("Sprint", SprintId?.ToString() ?? "Backlog", sprintId?.ToString() ?? "Backlog", byUserId);
        SprintId = sprintId;
        Touch();
    }

    public void SetDueDate(DateTime? dueDate, int byUserId)
    {
        RecordHistory("DueDate", DueDate?.ToString("yyyy-MM-dd") ?? "None", dueDate?.ToString("yyyy-MM-dd") ?? "None", byUserId);
        DueDate = dueDate;
        Touch();
    }

    public void SetParent(int? parentId) { ParentIssueId = parentId; Touch(); }
    public void SetEpic(int? epicId) { EpicId = epicId; Touch(); }
    public void Resolve() { ResolutionDate = DateTime.UtcNow; Touch(); }
    public void SetLabels(string labelsJson) { Labels = labelsJson; Touch(); }
    public void LogTime(int minutesSpent) { TimeSpentMinutes = (TimeSpentMinutes ?? 0) + minutesSpent; Touch(); }

    private void RecordHistory(string field, string oldValue, string newValue, int userId)
    {
        // History records are added after the issue is persisted via repository
        // This is a placeholder — in production use domain events
        _ = field; _ = oldValue; _ = newValue; _ = userId;
    }
}
