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

                    // Today’s scheduled time = 6 AM IST
                    var todayRunIst = new DateTime(nowIst.Year, nowIst.Month, nowIst.Day, 6, 0, 0);

                    // If already past 6 AM, schedule next run for tomorrow
                    if (nowIst > todayRunIst)
                        todayRunIst = todayRunIst.AddDays(1);

                    // Refresh current time in IST
                    var runIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _indiaTimeZone);

                    // ⛔ Skip if Sunday or Monday
                    if (runIst.DayOfWeek is DayOfWeek.Sunday or DayOfWeek.Monday)
                    {
                        _logger.LogInformation("Skipping NAV job on {Day}", runIst.DayOfWeek);
                    }
                    else
                    {
                        // ✅ Calculate correct market date (yesterday)
                        var marketDate = runIst.AddDays(-1);

                        using var scope = _services.CreateScope();
                        var holidayProvider = scope.ServiceProvider.GetRequiredService<IMarketHolidayProvider>();
                        var downloader = scope.ServiceProvider.GetRequiredService<IAmfiExcelDownloadService>();
                        string formattedDate = marketDate.ToString("yyyy-MM-dd");
                        // ✅ Check if already processed today (file exists)
                        string filePath = Path.Combine("DataFiles", $"nav-{formattedDate:yyyyMMdd}.xlsx");
                        if (File.Exists(filePath))
                        {
                            _logger.LogInformation("Skipping NAV job for {Date} - File already exists", marketDate.ToString("yyyy-MM-dd"));
                        }
                        else if (await holidayProvider.IsHolidayAsync(marketDate))
                        {
                            _logger.LogInformation("Skipping NAV job for {Date} (Market Holiday)", marketDate.ToString("yyyy-MM-dd"));
                        }
                        else
                        {
                            // ✅ Run job
                            _logger.LogInformation("Running AMFI NAV job for market date {Date}", marketDate.ToString("yyyy-MM-dd"));
                            await downloader.DownloadAndSaveAsync(marketDate);                         
                        }
                    }

                    // ⏰ Always wait until next day 6 AM
                    var delay = todayRunIst - nowIst;
                    if (delay < TimeSpan.Zero) delay = TimeSpan.FromHours(24);

                    _logger.LogInformation("Next AMFI NAV job scheduled at {Time} IST", todayRunIst);
                    await Task.Delay(delay, stoppingToken);
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
