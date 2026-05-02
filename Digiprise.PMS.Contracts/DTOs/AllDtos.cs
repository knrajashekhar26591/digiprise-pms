namespace Digiprise.PMS.Contracts.DTOs;

// ── Project DTOs ──────────────────────────────────────────────────────
public record ProjectDto(
    int Id, string Key, string Name, string? Description,
    string? Icon, string? Color, string BoardType, string Status,
    int LeadUserId, string? LeadName, DateTime CreatedAt, DateTime? TargetDate);

// ── Issue DTOs ────────────────────────────────────────────────────────
public record IssueDto(
    int Id, string IssueKey, string IssueType, string Summary,
    string? Description, int ProjectId, string ProjectKey,
    int StatusId, string StatusName, string StatusCategory,
    string Priority, int? AssigneeId, string? AssigneeName,
    int ReporterId, string ReporterName, int? ParentIssueId,
    int? SprintId, string? SprintName, int? StoryPoints,
    DateTime? StartDate, DateTime? DueDate, string[]? Labels, DateTime CreatedAt, DateTime UpdatedAt);

public record IssueListItemDto(
    int Id, string IssueKey, string IssueType, string Summary,
    string Priority, string StatusName, string StatusCategory,
    int? AssigneeId, string? AssigneeName, int? StoryPoints, DateTime UpdatedAt);

// ── Sprint DTOs ───────────────────────────────────────────────────────
public record SprintDto(
    int Id, int ProjectId, string Name, string? Goal,
    DateTime? StartDate, DateTime? EndDate, string State,
    int? VelocityPoints, int IssueCount, int TotalStoryPoints);

// ── User DTOs ─────────────────────────────────────────────────────────
public record UserDto(int Id, string Email, string DisplayName, string? AvatarUrl, bool IsActive);

// ── Comment DTOs ──────────────────────────────────────────────────────
public record CommentDto(int Id, int IssueId, int UserId, string AuthorName, string? AvatarUrl, string Body, DateTime CreatedAt, DateTime UpdatedAt);

// ── Notification DTOs ─────────────────────────────────────────────────
public record NotificationDto(int Id, string Title, string Body, bool IsRead, string? EntityType, int? EntityId, DateTime CreatedAt);

// ── Auth DTOs ─────────────────────────────────────────────────────────
public record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);

// ── Dashboard DTOs ────────────────────────────────────────────────────
public record IssueStatDto(string Label, int Count);
public record BurndownPointDto(DateTime Date, int Remaining, int Ideal);
public record DashboardSummaryDto(int TotalIssues, int OpenIssues, int InProgressIssues, int DoneIssues, IEnumerable<IssueStatDto> ByPriority);

// ── Phase 3: Enterprise Module DTOs ───────────────────────────────────
public record BudgetDto(int Id, int ProjectId, string Name, decimal MaterialBudgetCents, decimal LaborBudgetCents, string? Description);
public record CostTypeDto(int Id, string Name, string? Description, bool IsDefault);
public record CostEntryDto(int Id, int IssueId, string IssueKey, int UserId, string UserName, int CostTypeId, string CostTypeName, DateTime SpentOn, decimal Units, long UnitCostCents, string? Comment);
public record SlaPolicyDto(int Id, int ProjectId, string Name, string? TargetPriority, int ResponseSecs, int ResolutionSecs);
public record SlaBreachDto(long Id, int IssueId, int PolicyId, string Type, string State, DateTimeOffset StartedAt, DateTimeOffset BreachAt, DateTimeOffset? BreachedAt);
public record AutomationRuleDto(int Id, int ProjectId, string Name, bool IsActive, string TriggerConfig, string ConditionConfig, string ActionConfig, DateTime? LastRunAt);
