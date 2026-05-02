using Digiprise.PMS.Application.Interfaces;
using Digiprise.PMS.Domain.Events;

namespace Digiprise.PMS.Application.Handlers;

public class AutomationEventHandler : 
    INotificationHandler<IssueStatusChangedEvent>
{
    private readonly IAutomationService _automation;

    public AutomationEventHandler(IAutomationService automation)
    {
        _automation = automation;
    }

    public async Task Handle(IssueStatusChangedEvent domainEvent, CancellationToken ct)
    {
        await _automation.ExecuteAsync("StatusTransitioned", domainEvent, domainEvent.TenantId, ct);
    }
}
