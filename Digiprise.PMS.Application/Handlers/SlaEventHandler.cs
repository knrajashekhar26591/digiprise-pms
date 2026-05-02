using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Domain.Events;

namespace Digiprise.PMS.Application.Handlers;

public class SlaEventHandler : 
    INotificationHandler<IssueCreatedEvent>,
    INotificationHandler<IssueStatusChangedEvent>
{
    private readonly ISlaMonitorService _slaMonitor;

    public SlaEventHandler(ISlaMonitorService slaMonitor)
    {
        _slaMonitor = slaMonitor;
    }

    public async Task Handle(IssueCreatedEvent domainEvent, CancellationToken ct)
    {
        await _slaMonitor.EvaluateIssueSlaAsync(domainEvent.IssueId, domainEvent.TenantId, ct);
    }

    public async Task Handle(IssueStatusChangedEvent domainEvent, CancellationToken ct)
    {
        // Re-evaluate SLA on status change (e.g. if entering/leaving a pause state)
        await _slaMonitor.EvaluateIssueSlaAsync(domainEvent.IssueId, domainEvent.TenantId, ct);
    }
}
