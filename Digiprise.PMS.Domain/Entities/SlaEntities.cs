using Digiprise.PMS.Domain.Enums;
using Digiprise.PMS.Domain.Interfaces;

namespace Digiprise.PMS.Domain.Entities;

public class SlaPolicy : BaseEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Priority? TargetPriority { get; private set; } // Null means applies to all priorities
    public int ResponseSecs { get; private set; }
    public int ResolutionSecs { get; private set; }
    public string? PauseStatuses { get; private set; } // JSON array of StatusIds where clock stops


    public Project? Project { get; private set; }

    protected SlaPolicy() { }

    public static SlaPolicy Create(int tenantId, int projectId, string name, Priority? targetPriority, int responseSecs, int resolutionSecs, string? pauseStatuses = null)
    {
        return new SlaPolicy
        {
            TenantId = tenantId,
            ProjectId = projectId,
            Name = name,
            TargetPriority = targetPriority,
            ResponseSecs = responseSecs,
            ResolutionSecs = resolutionSecs,
            PauseStatuses = pauseStatuses
        };
    }

    public void UpdatePauseStatuses(string? json) { PauseStatuses = json; Touch(); }

}

public class SlaBreach : BaseEntity
{
    public int IssueId { get; private set; }
    public int PolicyId { get; private set; }
    public string Type { get; private set; } = "Resolution"; // Response, Resolution
    public string State { get; private set; } = "WithinGoal"; // WithinGoal, Breaching, Breached
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset BreachAt { get; private set; }
    public int PausedSeconds { get; private set; }
    public DateTimeOffset? BreachedAt { get; private set; } // When it actually breached

    public Issue? Issue { get; private set; }
    public SlaPolicy? Policy { get; private set; }

    protected SlaBreach() { }

    public static SlaBreach Create(int issueId, int policyId, string type, DateTimeOffset startedAt, int targetSecs)
    {
        return new SlaBreach
        {
            IssueId = issueId,
            PolicyId = policyId,
            Type = type,
            StartedAt = startedAt,
            BreachAt = startedAt.AddSeconds(targetSecs),
            State = "WithinGoal",
            PausedSeconds = 0
        };
    }

    public void AddPauseTime(int seconds)
    {
        if (State == "Breached") return;
        PausedSeconds += seconds;
        BreachAt = BreachAt.AddSeconds(seconds);
        Touch();
    }

    public void MarkBreached()
    {
        if (State == "Breached") return;
        State = "Breached";
        BreachedAt = DateTimeOffset.UtcNow;
        Touch();
    }

}
