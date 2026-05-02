using System.Text.Json;
using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Digiprise.PMS.Application.Services;

public class SlaMonitorService : ISlaMonitorService
{
    private readonly ISlaRepository _sla;
    private readonly IIssueRepository _issues;
    private readonly ILogger<SlaMonitorService> _logger;

    public SlaMonitorService(ISlaRepository sla, IIssueRepository issues, ILogger<SlaMonitorService> logger)
    {
        _sla = sla;
        _issues = issues;
        _logger = logger;
    }

    public async Task ProcessBreachesAsync(CancellationToken ct = default)
    {
        var activeBreaches = await _sla.GetActiveBreachesAsync(ct);
        
        foreach (var breach in activeBreaches)
        {
            if (breach.Issue == null || breach.Policy == null) continue;

            // Check if issue is in a pause status
            if (!string.IsNullOrEmpty(breach.Policy.PauseStatuses))
            {
                var pauseStatusIds = JsonSerializer.Deserialize<int[]>(breach.Policy.PauseStatuses);
                if (pauseStatusIds != null && pauseStatusIds.Contains(breach.Issue.StatusId))
                {
                    // Clock is paused - increment paused time (assuming 1 minute intervals for now)
                    breach.AddPauseTime(60); 
                    await _sla.UpdateBreachAsync(breach, ct);
                    continue;
                }
            }

            // Check if breached
            if (DateTimeOffset.UtcNow > breach.BreachAt)
            {
                breach.MarkBreached();
                await _sla.UpdateBreachAsync(breach, ct);
                _logger.LogWarning("SLA Breach detected for Issue {IssueId} on Policy {PolicyId}", breach.IssueId, breach.PolicyId);
            }
        }

        await _sla.SaveChangesAsync(ct);
    }

    public async Task EvaluateIssueSlaAsync(int issueId, int tenantId, CancellationToken ct = default)
    {
        var issue = await _issues.GetByIdAsync(issueId, ct);
        if (issue == null || issue.TenantId != tenantId) return;

        var policies = await _sla.GetByProjectAsync(issue.ProjectId, ct);
        var existingBreaches = await _sla.GetBreachesByIssueAsync(issueId, ct);

        foreach (var policy in policies)
        {
            // Check if policy applies to this priority
            if (policy.TargetPriority.HasValue && policy.TargetPriority != issue.Priority) continue;

            // Start Response SLA if not already started
            if (!existingBreaches.Any(b => b.PolicyId == policy.Id && b.Type == "Response"))
            {
                var responseBreach = SlaBreach.Create(issueId, policy.Id, "Response", issue.CreatedAt, policy.ResponseSecs);
                await _sla.AddBreachAsync(responseBreach, ct);
            }

            // Start Resolution SLA if not already started
            if (!existingBreaches.Any(b => b.PolicyId == policy.Id && b.Type == "Resolution"))
            {
                var resolutionBreach = SlaBreach.Create(issueId, policy.Id, "Resolution", issue.CreatedAt, policy.ResolutionSecs);
                await _sla.AddBreachAsync(resolutionBreach, ct);
            }
        }

        await _sla.SaveChangesAsync(ct);
    }
}
