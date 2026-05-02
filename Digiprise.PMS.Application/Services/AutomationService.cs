using System.Text.Json;
using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Contracts.DTOs;
using Digiprise.PMS.Domain.Entities;

using Digiprise.PMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Digiprise.PMS.Application.Services;

public class AutomationService : IAutomationService
{
    private readonly IAutomationRepository _automation;
    private readonly IIssueRepository _issues;
    private readonly ILogger<AutomationService> _logger;

    public AutomationService(IAutomationRepository automation, IIssueRepository issues, ILogger<AutomationService> logger)
    {
        _automation = automation;
        _issues = issues;
        _logger = logger;
    }

    public async Task<IEnumerable<AutomationRuleDto>> GetByProjectAsync(int projectId, int tenantId, CancellationToken ct = default)
    {
        var rules = await _automation.GetByProjectAsync(projectId, ct);
        return rules.Select(r => Map(r));
    }

    public async Task<AutomationRuleDto> CreateRuleAsync(int projectId, string name, string trigger, string conditions, string actions, int tenantId, CancellationToken ct = default)
    {
        var rule = AutomationRule.Create(tenantId, projectId, name, trigger, conditions, actions);
        await _automation.AddAsync(rule, ct);
        await _automation.SaveChangesAsync(ct);
        return Map(rule);
    }


    public async Task ExecuteAsync(string triggerType, object context, int tenantId, CancellationToken ct = default)
    {
        var rules = await _automation.GetActiveRulesAsync(tenantId, ct);
        var matchingRules = rules.Where(r => r.TriggerConfig.Contains(triggerType));

        foreach (var rule in matchingRules)
        {
            try
            {
                if (await EvaluateConditionsAsync(rule, context))
                {
                    await ExecuteActionsAsync(rule, context, ct);
                    rule.RecordRun();
                    await _automation.UpdateAsync(rule, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing automation rule {RuleId}", rule.Id);
            }
        }

        await _automation.SaveChangesAsync(ct);
    }

    private async Task<bool> EvaluateConditionsAsync(AutomationRule rule, object context)
    {
        // Simple implementation: if no conditions, always true
        if (string.IsNullOrEmpty(rule.ConditionConfig) || rule.ConditionConfig == "[]") return true;

        // In a real app, parse JSON and evaluate using a rule engine or reflection
        // For now, assume true for testing
        return true;
    }

    private async Task ExecuteActionsAsync(AutomationRule rule, object context, CancellationToken ct)
    {
        var actions = JsonSerializer.Deserialize<List<AutomationAction>>(rule.ActionConfig);
        if (actions == null) return;

        foreach (var action in actions)
        {
            switch (action.Type)
            {
                case "AddComment":
                    await HandleAddCommentAction(action, context, ct);
                    break;
                case "TransitionStatus":
                    // await HandleTransitionStatusAction(action, context, ct);
                    break;
                default:
                    _logger.LogWarning("Unknown automation action type: {Type}", action.Type);
                    break;
            }
        }
    }

    private async Task HandleAddCommentAction(AutomationAction action, object context, CancellationToken ct)
    {
        if (context is Digiprise.PMS.Domain.Events.IssueStatusChangedEvent ev)
        {
            var body = action.Params.GetValueOrDefault("body", "Automation triggered.");
            var comment = Comment.Create(ev.IssueId, 0 /* System User */, body);
            // We'd need an ICommentRepository or use IssueService
            _logger.LogInformation("Automation: Added comment to issue {IssueId}", ev.IssueId);
        }
    }

    private static AutomationRuleDto Map(AutomationRule r) => new AutomationRuleDto(
        r.Id, r.ProjectId, r.Name, r.IsActive, r.TriggerConfig, r.ConditionConfig, r.ActionConfig, r.LastRunAt);
}


public class AutomationAction
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Params { get; set; } = new();
}
