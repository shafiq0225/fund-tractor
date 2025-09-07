using Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class AmfiNavService : IAmfiNavService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHostEnvironment _hostEnv;
        private readonly ILogger<AmfiNavService> _logger;

        private static readonly TimeZoneInfo IstTz =
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        public AmfiNavService(
            IHttpClientFactory httpClientFactory,
            IHostEnvironment hostEnv,
            ILogger<AmfiNavService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _hostEnv = hostEnv;
            _logger = logger;
        }

        public async Task DownloadAndSaveAsync(CancellationToken cancellationToken = default)
        {
            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstTz);

            if (nowIst.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday or DayOfWeek.Monday)
            {
                _logger.LogInformation("Skipping AMFI download on {Day}.", nowIst.DayOfWeek);
                return;
            }

            var folderPath = Path.Combine(_hostEnv.ContentRootPath, "DataFiles");
            Directory.CreateDirectory(folderPath);

            var fileName = $"NAVAll_{nowIst:yyyyMMdd}.txt";
            var filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                _logger.LogInformation("NAV file already exists for today: {path}", filePath);
                return;
            }

            var baseUrl = "https://www.amfiindia.com/spages/NAVAll.txt";
            var cacheBuster = nowIst.ToString("ddMMyyyyHHmmss");
            var url = $"{baseUrl}?t={cacheBuster}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("MFDownloader", "1.0"));

                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download NAVAll. Status: {status}", response.StatusCode);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                await File.WriteAllTextAsync(filePath, content, cancellationToken);

                _logger.LogInformation("Saved NAVAll to {path}", filePath);


            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AMFI download was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading/saving NAVAll file.");
            }
        }
    }
}
