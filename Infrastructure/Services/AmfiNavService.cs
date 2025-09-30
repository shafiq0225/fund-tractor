using Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Infrastructure.Services
{
    public class AmfiNavService : IAmfiNavService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHostEnvironment _hostEnv;
        private readonly ILogger<AmfiNavService> _logger;
        private readonly TimeZoneInfo _indiaTimeZone;

        public AmfiNavService(
            IHttpClientFactory httpClientFactory,
            IHostEnvironment hostEnv,
            ILogger<AmfiNavService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _hostEnv = hostEnv;
            _logger = logger;
            _indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }

        public async Task DownloadAndSaveAsync(CancellationToken cancellationToken = default)
        {
            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _indiaTimeZone);

            // Skip Sundays and Mondays
            if (nowIst.DayOfWeek is DayOfWeek.Sunday or DayOfWeek.Monday)
            {
                _logger.LogInformation("Skipping NAV download on {Day}", nowIst.DayOfWeek);
                return;
            }

            var folderPath = Path.Combine(_hostEnv.ContentRootPath, "DataFiles");
            Directory.CreateDirectory(folderPath);

            var fileName = $"NAVAll_{nowIst:yyyyMMdd}.txt";
            var filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                _logger.LogInformation("NAV file already exists for today: {File}", filePath);
                return;
            }

            var url = $"https://portal.amfiindia.com/spages/NAVAll.txt";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MFDownloader", "1.0"));

                _logger.LogInformation("Downloading NAV file for {Date} from {Url}", nowIst.ToString("yyyy-MM-dd"), url);

                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download NAVAll for {Date}. Status: {Status}", nowIst.ToString("yyyy-MM-dd"), response.StatusCode);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                // Quick validation: AMFI NAVAll files always start with "Scheme Code"
                if (!content.StartsWith("Scheme Code"))
                {
                    _logger.LogWarning("Invalid NAV file received for {Date}. Content not valid.", nowIst.ToString("yyyy-MM-dd"));
                    return;
                }

                await File.WriteAllTextAsync(filePath, content, cancellationToken);
                _logger.LogInformation("Saved NAVAll to {Path}", filePath);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("NAV download was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while downloading NAVAll file.");
            }
        }
    }
}
