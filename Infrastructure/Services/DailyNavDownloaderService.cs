using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class DailyNavDownloaderService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DailyNavDownloaderService> _logger;
        private readonly TimeZoneInfo _indiaTimeZone;

        public DailyNavDownloaderService(IServiceProvider services, ILogger<DailyNavDownloaderService> logger)
        {
            _services = services;
            _logger = logger;
            _indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Current IST time
                    var nowUtc = DateTime.UtcNow;
                    var nowIst = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _indiaTimeZone);

                    // Schedule next 6 AM IST
                    var nextRunIst = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, 6, 0, 0);
                    if (nowIst > nextRunIst)
                        nextRunIst = nextRunIst.AddDays(1);

                    var delay = nextRunIst - nowIst;
                    _logger.LogInformation("Next AMFI NAV job scheduled at {Time} IST", nextRunIst);
                    await Task.Delay(delay, stoppingToken);

                    //Test Mode
                    //var delay = TimeSpan.FromSeconds(5);
                    //_logger.LogInformation("Next AMFI NAV job scheduled in 5 seconds (Test Mode)");
                    //await Task.Delay(delay, stoppingToken);

                    // Refresh current time in IST at run
                    var runIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _indiaTimeZone);

                    // ⛔ Skip if Sunday or Monday
                    if (runIst.DayOfWeek == DayOfWeek.Sunday || runIst.DayOfWeek == DayOfWeek.Monday)
                    {
                        _logger.LogInformation("Skipping NAV job on {Day}", runIst.DayOfWeek);
                        continue;
                    }

                    // ✅ Calculate correct market date (yesterday)
                    var marketDate = runIst.AddDays(-1);

                    using var scope = _services.CreateScope();

                    var holidayProvider = scope.ServiceProvider.GetRequiredService<IMarketHolidayProvider>();

                    var downloader = scope.ServiceProvider.GetRequiredService<IAmfiExcelDownloadService>();

                    if (await holidayProvider.IsHolidayAsync(marketDate))
                    {
                        _logger.LogInformation("Skipping NAV job for {Date} (Market Holiday)", marketDate.ToString("yyyy-MM-dd"));
                        continue;
                    }

                    _logger.LogInformation("Running AMFI NAV job for market date {Date}", marketDate.ToString("yyyy-MM-dd"));
                    await downloader.DownloadAndSaveAsync(marketDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Daily NAV background service.");
                }
            }

        }
        private DateTime GetMarketDate(DateTime todayIst)
        {
            // If today is Sunday → last Friday
            if (todayIst.DayOfWeek == DayOfWeek.Sunday)
                return todayIst.AddDays(-2);

            // If today is Monday → last Friday
            if (todayIst.DayOfWeek == DayOfWeek.Monday)
                return todayIst.AddDays(-3);

            // Otherwise → yesterday’s data
            return todayIst.AddDays(-1);
        }

    }
}
