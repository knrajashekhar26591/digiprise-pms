using System.Text.Json;
using Digiprise.PMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Digiprise.PMS.Infrastructure.Data.Interceptors;

public class IssueJournalInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = dbContext.ChangeTracker.Entries<Issue>()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
            .ToList();

        var journals = new List<IssueJournal>();

        foreach (var entry in entries)
        {
            var issue = entry.Entity;
            var changedFields = new Dictionary<string, object?>();

            if (entry.State == EntityState.Added)
            {
                changedFields.Add("Created", true);
            }
            else
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified && prop.Metadata.Name != "UpdatedAt")
                    {
                        changedFields.Add(prop.Metadata.Name, new
                        {
                            Old = prop.OriginalValue,
                            New = prop.CurrentValue
                        });
                    }
                }
            }

            if (changedFields.Any())
            {
                // In a real app we'd get ActorId from an ICurrentUserService
                // For now, we fallback to 1 or if we can extract it from AuditLogs
                int actorId = 1; 

                // Get the next JournalNumber for this Issue
                int nextJournalNumber = 1;
                var lastJournal = await dbContext.Set<IssueJournal>()
                    .Where(j => j.IssueId == issue.Id)
                    .OrderByDescending(j => j.JournalNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lastJournal != null)
                {
                    nextJournalNumber = lastJournal.JournalNumber + 1;
                }

                journals.Add(IssueJournal.Create(
                    issue.TenantId,
                    issue.Id,
                    nextJournalNumber,
                    actorId,
                    JsonSerializer.Serialize(changedFields)
                ));
            }
        }

        if (journals.Any())
        {
            await dbContext.Set<IssueJournal>().AddRangeAsync(journals, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
