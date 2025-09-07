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

                if (nowIst.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday or DayOfWeek.Monday)
                    && nowIst.Hour == 6 && nowIst.Minute == 30)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var amfiNavService = scope.ServiceProvider.GetRequiredService<IAmfiNavService>();

                    try
                    {
                        _logger.LogInformation("Downloading AMFI NAV at {Time}", nowIst);
                        await amfiNavService.DownloadAndSaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in AMFI NAV download");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

}
