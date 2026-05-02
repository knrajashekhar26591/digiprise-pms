using Digiprise.PMS.Application.Services;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Digiprise.PMS.Tests;

public class EnterpriseTests
{
    private readonly Mock<ISlaRepository> _slaRepo = new();
    private readonly Mock<IAutomationRepository> _autoRepo = new();
    private readonly Mock<IIssueRepository> _issueRepo = new();
    private readonly Mock<ILogger<SlaMonitorService>> _slaLogger = new();
    private readonly Mock<ILogger<AutomationService>> _autoLogger = new();

    [Fact]
    public async Task SlaMonitor_ShouldProcessBreaches()
    {
        // Arrange
        var monitor = new SlaMonitorService(_slaRepo.Object, _issueRepo.Object, _slaLogger.Object);
        var breaches = new List<SlaBreach> { 
            SlaBreach.Create(1, 1, "Resolution", DateTimeOffset.UtcNow.AddHours(-1)) 
        };
        _slaRepo.Setup(r => r.GetActiveBreachesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(breaches);

        // Act
        await monitor.ProcessBreachesAsync();

        // Assert
        _slaRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutomationService_ShouldExecuteMatchingRules()
    {
        // Arrange
        var service = new AutomationService(_autoRepo.Object, _issueRepo.Object, _autoLogger.Object);
        var rules = new List<AutomationRule> {
            AutomationRule.Create(1, "Test Rule", "StatusTransitioned", "[]", "[]")
        };
        _autoRepo.Setup(r => r.GetActiveRulesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(rules);

        // Act
        await service.ExecuteAsync("StatusTransitioned", new { IssueId = 1 }, 1);

        // Assert
        _autoRepo.Verify(r => r.UpdateAsync(It.IsAny<AutomationRule>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
