using Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class MarketHolidayProvider : IMarketHolidayProvider
    {
        private readonly ILogger<MarketHolidayProvider> _logger;
        private readonly HttpClient _httpClient;
        // Cache of holidays by year
        private readonly Dictionary<int, HashSet<DateTime>> _holidaysCache = new();

        public MarketHolidayProvider(ILogger<MarketHolidayProvider> logger, HttpClient httpClient = null)
        {
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<bool> IsHolidayAsync(DateTime date)
        {
            date = date.Date;
            int year = date.Year;

            if (!_holidaysCache.TryGetValue(year, out var set))
            {
                set = await LoadHolidaysForYearAsync(year);
                _holidaysCache[year] = set;
            }

            return set.Contains(date);
        }

        private async Task<HashSet<DateTime>> LoadHolidaysForYearAsync(int year)
        {
            var holidays = new HashSet<DateTime>();

            try
            {
                string url = $"https://api.upstox.com/v2/market/holidays?year={year}";
                _logger.LogInformation("Fetching Upstox holidays for year {Year}: {Url}", year, url);

                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Upstox holidays API returned status {Status} for year {Year}", resp.StatusCode, year);
                    return holidays;
                }

                var json = await resp.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElem.EnumerateArray())
                    {
                        // Expected item fields: e.g. "date", and maybe a list of exchanges, etc.
                        if (item.TryGetProperty("date", out var dateElem))
                        {
                            var dateString = dateElem.GetString();
                            if (!string.IsNullOrEmpty(dateString))
                            {
                                if (DateTime.TryParse(dateString, out var dt))
                                {
                                    holidays.Add(dt.Date);
                                }
                                else
                                {
                                    _logger.LogDebug("Unable to parse holiday date string: {DateString}", dateString);
                                }
                            }
                        }
                    }
                    _logger.LogInformation("Loaded {Count} holidays from Upstox API for {Year}", holidays.Count, year);
                }
                else
                {
                    // Case where "data" is missing or empty
                    _logger.LogWarning("Upstox holidays API for year {Year} returned no data field or empty array", year);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Upstox holidays for year {Year}", year);
            }

            return holidays;
        }

    }
}
