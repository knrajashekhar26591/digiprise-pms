using Digiprise.PMS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Digiprise.PMS.Infrastructure.Services;

public class SlaBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaBackgroundWorker> _logger;

    public SlaBackgroundWorker(IServiceProvider serviceProvider, ILogger<SlaBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Background Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var slaMonitor = scope.ServiceProvider.GetRequiredService<ISlaMonitorService>();
                    await slaMonitor.ProcessBreachesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing SLA breaches.");
            }

            // Wait for 1 minute before next run
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("SLA Background Worker is stopping.");
    }
}
