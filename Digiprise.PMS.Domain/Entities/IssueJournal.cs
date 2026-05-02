using Digiprise.PMS.Domain.Entities;

using Digiprise.PMS.Domain.Interfaces;

namespace Digiprise.PMS.Domain.Entities;

/// <summary>
/// Immutable changelog for every Issue field change.
/// Powers the Baseline Comparison API as outlined in the v2.0 OpenProject Architecture upgrade.
/// </summary>
public class IssueJournal : BaseEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int IssueId { get; private set; }
    public int JournalNumber { get; private set; }
    public int ActorId { get; private set; }
    public string ChangedFields { get; private set; } = string.Empty; // JSON diff object

    protected IssueJournal() { }

    public static IssueJournal Create(int tenantId, int issueId, int journalNumber, int actorId, string changedFieldsJson)
    {
        return new IssueJournal
        {
            TenantId = tenantId,
            IssueId = issueId,
            JournalNumber = journalNumber,
            ActorId = actorId,
            ChangedFields = changedFieldsJson
        };
    }
}
