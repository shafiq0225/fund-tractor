using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class AmfiDownloader
    {
        private readonly ILogger<AmfiDownloader> _logger;
        private readonly HttpClient _httpClient;
        private readonly IHostEnvironment _hostEnv;
        //private readonly string _dataFolder;

        public AmfiDownloader(ILogger<AmfiDownloader> logger, IHostEnvironment hostEnv)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            // Save inside API project under api/datafiles
            //_dataFolder = Path.Combine(AppContext.BaseDirectory, "datafiles");
            //if (!Directory.Exists(_dataFolder))
            //{
            //    Directory.CreateDirectory(_dataFolder);
            //}

            _hostEnv = hostEnv;
        }

        public async Task DownloadAndSaveAsync(DateTime marketDate)
        {
            string formattedDate = marketDate.ToString("yyyy-MM-dd");
            //string url =
            //    $"https://www.amfiindia.com/api/download-nav-history?strMFID=all&schemeTypeDesc=all&FromDate={formattedDate}&ToDate={formattedDate}";

            string url =
                $"https://www.amfiindia.com/api/download-nav-history?strMFID=all&schemeTypeDesc=all&FromDate=2025-09-12&ToDate=2025-09-12";

            _logger.LogInformation("Downloading NAV Excel for {Date} from {Url}", formattedDate, url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            string fileName = $"nav-{formattedDate}.xlsx";

            var folderPath = Path.Combine(_hostEnv.ContentRootPath, "DataFiles");
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName);

            await File.WriteAllBytesAsync(filePath, bytes);

            _logger.LogInformation("Saved NAV file: {FilePath}", filePath);
        }
    }
}
