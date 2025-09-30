using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class AmfiNavBackgroundService : BackgroundService
{
    private readonly ILogger<AmfiNavBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeZoneInfo _indiaTimeZone;

    public AmfiNavBackgroundService(
        ILogger<AmfiNavBackgroundService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _indiaTimeZone);

            // ✅ Today’s run time = 12:05 AM IST
            var todayRunTime = nowIst.Date.AddMinutes(5);

            // Run immediately if past scheduled time
            if (nowIst >= todayRunTime)
            {
                using var scope = _scopeFactory.CreateScope();
                var navService = scope.ServiceProvider.GetRequiredService<IAmfiNavService>();
                await navService.DownloadAndSaveAsync(stoppingToken);

                todayRunTime = todayRunTime.AddDays(1);
            }

            var delay = todayRunTime - nowIst;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.FromHours(24);

            _logger.LogInformation("Next NAV check scheduled at {Time} IST", todayRunTime);
            await Task.Delay(delay, stoppingToken);
        }
    }
}
