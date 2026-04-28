using System.ComponentModel.DataAnnotations;

namespace Digiprise.PMS.Contracts.Requests;

// ── Auth ──────────────────────────────────────────────────────────────
public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required] public string TenantSubdomain { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string DisplayName { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

// ── Projects ──────────────────────────────────────────────────────────
public class CreateProjectRequest
{
    [Required, MinLength(2), MaxLength(10)] public string Key { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string BoardType { get; set; } = "Scrum";
    public DateTime? StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
}

public class UpdateProjectRequest
{
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
}

// ── Issues ────────────────────────────────────────────────────────────
public class CreateIssueRequest
{
    [Required] public string ProjectKey { get; set; } = string.Empty;
    [Required] public string IssueType { get; set; } = "Story";
    [Required, MaxLength(255)] public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Major";
    public int? AssigneeId { get; set; }
    public int? StoryPoints { get; set; }
    public int? SprintId { get; set; }
    public int? ParentIssueId { get; set; }
    public string[]? Labels { get; set; }
    public DateTime? DueDate { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
}

public class UpdateIssueRequest
{
    [MaxLength(255)] public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public int? AssigneeId { get; set; }
    public int? StoryPoints { get; set; }
    public int? SprintId { get; set; }
    public string[]? Labels { get; set; }
    public DateTime? DueDate { get; set; }
}

public class TransitionIssueRequest
{
    [Required] public int TransitionId { get; set; }
    public string? Comment { get; set; }
}

public class IssueSearchRequest
{
    public string? Iql { get; set; }
    public int MaxResults { get; set; } = 50;
    public int StartAt { get; set; } = 0;
    public string? OrderBy { get; set; }
    public bool Descending { get; set; } = true;
}

public class BulkIssueRequest
{
    [Required] public int[] IssueIds { get; set; } = Array.Empty<int>();
    public string? Action { get; set; } // Assign, ChangeStatus, SetPriority, AddLabel, MoveToSprint
    public string? Value { get; set; }
}

// ── Sprints ───────────────────────────────────────────────────────────
public class CreateSprintRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class StartSprintRequest
{
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    public string? Goal { get; set; }
}

// ── Comments ──────────────────────────────────────────────────────────
public class CreateCommentRequest
{
    [Required] public string Body { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
}

// ── Attachments ───────────────────────────────────────────────────────
public class UploadAttachmentRequest
{
    [Required] public string FileName { get; set; } = string.Empty;
    [Required] public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

// ── Members ───────────────────────────────────────────────────────────
public class AddMemberRequest
{
    [Required] public int UserId { get; set; }
    [Required] public string Role { get; set; } = "Developer";
}
