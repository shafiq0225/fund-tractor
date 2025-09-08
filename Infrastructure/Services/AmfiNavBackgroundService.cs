using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class AmfiNavBackgroundService : BackgroundService
    {
        private readonly ILogger<AmfiNavBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AmfiNavBackgroundService(
            ILogger<AmfiNavBackgroundService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                // Skip Sat, Sun, Mon
                if (nowIst.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday or DayOfWeek.Monday)
                {
                    _logger.LogInformation("Skipping job on {Day}", nowIst.DayOfWeek);
                }
                else
                {
                    try
                    {
                        _logger.LogInformation("Downloading AMFI NAV at {Time}", nowIst);

                        using var scope = _scopeFactory.CreateScope();
                        var amfiNavService = scope.ServiceProvider.GetRequiredService<IAmfiNavService>();
                        await amfiNavService.DownloadAndSaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in AMFI NAV download");
                    }
                }

                // Calculate next run at 6:30 AM IST tomorrow
                var tomorrowIst = nowIst.Date.AddDays(1).AddHours(6).AddMinutes(30);
                var delay = tomorrowIst - nowIst;

                if (delay < TimeSpan.Zero) delay = TimeSpan.FromHours(24); // fallback

                _logger.LogInformation("Next run scheduled after {Delay}", delay);

                await Task.Delay(delay, stoppingToken);
            }
        }

    }

}
