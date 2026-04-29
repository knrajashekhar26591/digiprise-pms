using Digiprise.PMS.Domain.Enums;

namespace Digiprise.PMS.Domain.Entities;

public class Project : BaseEntity
{
    public int TenantId { get; private set; }
    public string Key { get; private set; } = string.Empty; // e.g. DIG, CRM
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public int LeadUserId { get; private set; }
    public BoardType BoardType { get; private set; } = BoardType.Scrum;
    public ProjectStatus Status { get; private set; } = ProjectStatus.Active;
    public DateTime? StartDate { get; private set; }
    public DateTime? TargetDate { get; private set; }
    public string? Category { get; private set; }
    public int? WorkflowSchemeId { get; private set; }

    private readonly List<Issue> _issues = new();
    private readonly List<Sprint> _sprints = new();
    private readonly List<ProjectMember> _members = new();

    public IReadOnlyCollection<Issue> Issues => _issues.AsReadOnly();
    public IReadOnlyCollection<Sprint> Sprints => _sprints.AsReadOnly();
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    public Tenant? Tenant { get; private set; }
    public User? Lead { get; private set; }

    protected Project() { }

    public static Project Create(int tenantId, string key, string name, int leadUserId, BoardType boardType = BoardType.Scrum)
    {
        if (key.Length < 2 || key.Length > 10 || !key.All(char.IsLetterOrDigit))
            throw new ArgumentException("Project key must be 2-10 alphanumeric characters.");

        return new Project
        {
            TenantId = tenantId,
            Key = key.ToUpperInvariant(),
            Name = name,
            LeadUserId = leadUserId,
            BoardType = boardType,
            Status = ProjectStatus.Active
        };
    }

    public void Update(string name, string? description, string? icon, string? color, string? category, DateTime? startDate, DateTime? targetDate)
    {
        Name = name;
        Description = description;
        Icon = icon;
        Color = color;
        Category = category;
        StartDate = startDate;
        TargetDate = targetDate;
        Touch();
    }

    public void Archive() { Status = ProjectStatus.Archived; Touch(); }
    public void Delete() { Status = ProjectStatus.Deleted; Touch(); }
    public void Restore() { Status = ProjectStatus.Active; Touch(); }
    public void SetWorkflowScheme(int schemeId) { WorkflowSchemeId = schemeId; Touch(); }
}
