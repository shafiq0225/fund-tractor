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

                var todayDate = nowIst.Date;
                var runTimeToday = todayDate.AddHours(6).AddMinutes(30); // 6:30 AM IST

                // Check if file already exists for today
                // $"NAVAll_{nowIst:yyyyMMdd}.txt"
                string filePath = Path.Combine("DataFiles", $"NAVAll_{todayDate:yyyyMMdd}.txt");

                if (!File.Exists(filePath) && nowIst >= runTimeToday)
                {
                    if (nowIst.DayOfWeek is DayOfWeek.Sunday or DayOfWeek.Monday)
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
                }
                else
                {
                    _logger.LogInformation("Skipping execution — file already exists for today: {File}", filePath);
                }

                // Next check is tomorrow 6:30 AM IST
                var nextRunIst = todayDate.AddDays(1).AddHours(6).AddMinutes(30);
                var delay = nextRunIst - nowIst;

                _logger.LogInformation("Next check scheduled after {Delay}", delay);
                await Task.Delay(delay, stoppingToken);
            }
        }

    }

}
