using System.Globalization;
using ClosedXML.Excel;
using Core.DTOs;
using Core.Entities.AMFI;
using Core.Helpers;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AmfiRepository(StoreContext storeContext) : IAmfiRepository
{
    public async Task<List<ApprovedData>> GetApprovedSchemesAsync()
        => await storeContext.ApprovedData.Where(x => x.IsApproved).ToListAsync();

    public async Task AddSchemeDetailsAsync(List<SchemeDetail> schemes)
    {
        storeContext.SchemeDetails.AddRange(schemes);
        await storeContext.SaveChangesAsync();
    }

    public async Task ImportAmfiDataAsync(string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData))
            throw new ArgumentException("Raw data cannot be null or empty.", nameof(rawData));

        List<ApprovedData> approvedSchemes;
        try
        {
            approvedSchemes = await GetApprovedSchemesAsync() ?? new List<ApprovedData>();
        }
        catch (Exception ex)
        {
            // Log and rethrow or wrap
            throw new InvalidOperationException("Failed to fetch approved schemes.", ex);
        }

        var approvedSchemeCodes = approvedSchemes.Select(x => x.SchemeCode).ToHashSet();
        var lines = rawData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        string currentFundName = string.Empty;
        string currentFundId = string.Empty;

        foreach (var line in lines)
        {
            try
            {
                if (line.StartsWith("Scheme Code") || line.StartsWith("Open Ended") || line.StartsWith(" "))
                    continue; // skip headers

                if (AmfiDataHelper.IsFundLine(line))
                {
                    currentFundName = line.Trim();
                    currentFundId = AmfiDataHelper.GenerateFundId(currentFundName);
                    continue; // move to next line
                }

                var parts = line.Split(';');
                if (parts.Length < 6) continue;

                var schemeCode = parts[0].Trim();
                if (!approvedSchemeCodes.Contains(schemeCode)) continue;

                var schemeName = parts[3].Trim();
                var nav = decimal.TryParse(parts[4], out var parsedNav) ? parsedNav : 0;
                var date = DateTime.TryParse(parts[5], out var parsedDate) ? parsedDate : DateTime.Now;

                var existingScheme = await storeContext.SchemeDetails
                    .FirstOrDefaultAsync(x => x.SchemeCode == schemeCode && x.Date == parsedDate);

                if (existingScheme == null)
                {
                    await storeContext.SchemeDetails.AddAsync(new SchemeDetail
                    {
                        FundCode = currentFundId,
                        FundName = currentFundName,
                        SchemeCode = schemeCode,
                        SchemeName = schemeName,
                        Nav = nav,
                        Date = parsedDate,
                        IsVisible = true
                    });
                }
                else
                {
                    existingScheme.Nav = nav;
                    existingScheme.SchemeName = schemeName;
                    existingScheme.FundName = currentFundName;
                }
            }
            catch (Exception ex)
            {
                // Log the line that failed, continue with others
                // e.g., _logger.LogError(ex, "Failed to process line: {Line}", line);
                continue;
            }
        }

        try
        {
            await storeContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log and rethrow/wrap
            throw new InvalidOperationException("Failed to save scheme details.", ex);
        }
    }

    public async Task<(bool Success, string Message)> AddApprovedSchemeAsync(ApprovedSchemeDto addSchemeDto)
    {
        if (string.IsNullOrWhiteSpace(addSchemeDto.FundName))
            return (false, "Fund name is required.");

        if (string.IsNullOrWhiteSpace(addSchemeDto.SchemeId))
            return (false, "Scheme ID is required.");

        string fundId;
        try
        {
            fundId = AmfiDataHelper.GenerateFundId(addSchemeDto.FundName);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to generate FundId. Reason: {ex.Message}");
        }

        try
        {
            // ✅ Check if record already exists
            var exists = await storeContext.ApprovedData
                .AnyAsync(x => x.FundCode == fundId && x.SchemeCode == addSchemeDto.SchemeId);

            if (exists)
                return (false, "Record already exists.");

            var approvedFund = new ApprovedData
            {
                ApprovedName = "Shafiq",
                FundCode = fundId,
                IsApproved = addSchemeDto.IsApproved,
                SchemeCode = addSchemeDto.SchemeId,
                SchemeName = addSchemeDto.SchemeName,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedDate = DateTime.UtcNow
            };

            await storeContext.ApprovedData.AddAsync(approvedFund);
            await storeContext.SaveChangesAsync();

            return (true, "Inserted successfully.");
        }
        catch (DbUpdateException dbEx)
        {
            return (false, $"Database update failed. Reason: {dbEx.InnerException?.Message ?? dbEx.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error occurred. Reason: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateApprovedSchemeAsync(string fundId, string schemeId, bool isApproved)
    {
        if (string.IsNullOrWhiteSpace(fundId))
            return (false, "FundId is required");

        if (string.IsNullOrWhiteSpace(schemeId))
            return (false, "SchemeId is required");

        try
        {
            var existingRecord = await storeContext.ApprovedData
                .FirstOrDefaultAsync(x => x.FundCode == fundId && x.SchemeCode == schemeId);

            if (existingRecord == null)
                return (false, "Record not found");

            if (existingRecord.IsApproved == isApproved)
                return (false, "No changes made");

            // ✅ Update ApprovedData
            existingRecord.IsApproved = isApproved;
            existingRecord.LastUpdatedDate = DateTime.UtcNow;
            storeContext.ApprovedData.Update(existingRecord);

            // ✅ Sync visibility in SchemeDetails (only if record exists)
            var schemaDetails = await storeContext.SchemeDetails
                .FirstOrDefaultAsync(x => x.FundCode == fundId && x.SchemeCode == schemeId);

            if (schemaDetails != null)
            {
                schemaDetails.IsVisible = isApproved;
                storeContext.SchemeDetails.Update(schemaDetails);
            }

            await storeContext.SaveChangesAsync();
            return (true, "Updated successfully");
        }
        catch (DbUpdateException dbEx)
        {
            return (false, $"Database update error: {dbEx.InnerException?.Message ?? dbEx.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateApprovedFundAsync(string fundId, bool isApproved)
    {
        if (string.IsNullOrWhiteSpace(fundId))
            return (false, "FundId is required");

        try
        {
            var existingRecords = await storeContext.ApprovedData
                .Where(x => x.FundCode == fundId)
                .ToListAsync();

            if (existingRecords.Count == 0)
                return (false, "Record not found");

            foreach (var record in existingRecords)
            {
                record.IsApproved = isApproved;
                record.LastUpdatedDate = DateTime.UtcNow;
            }

            var schemeDetails = await storeContext.SchemeDetails
                .Where(x => x.FundCode == fundId)
                .ToListAsync();

            foreach (var scheme in schemeDetails)
            {
                scheme.IsVisible = isApproved;
            }

            await storeContext.SaveChangesAsync();

            return (true, "Updated successfully");
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "Concurrency conflict: the record was modified by another user. Please retry.");
        }
        catch (DbUpdateException dbEx)
        {
            return (false, $"Database update error: {dbEx.InnerException?.Message ?? dbEx.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, List<SchemeDetail>? Data)> GetSchemesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            return (false, "Start date cannot be later than end date.", null);
        }

        try
        {
            var result = await storeContext.SchemeDetails
                .Where(x => x.Date >= startDate && x.Date <= endDate && x.IsVisible)
                .OrderBy(x => x.Date)
                .ToListAsync();

            if (result.Count == 0)
                return (false, "No records found for the given date range.", new List<SchemeDetail>());

            return (true, "Records retrieved successfully.", result);
        }
        catch (OperationCanceledException)
        {
            return (false, "The request was canceled before completion.", null);
        }
        catch (DbUpdateException dbEx)
        {
            return (false, $"Database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}", null);
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error: {ex.Message}", null);
        }
    }

    public async Task ImportAmfiDataFromExcelAsync(byte[] excelData)
    {
        if (excelData == null || excelData.Length == 0)
            throw new ArgumentException("Excel data cannot be null or empty.", nameof(excelData));

        List<ApprovedData> approvedSchemes = await GetApprovedSchemesAsync() ?? new List<ApprovedData>();
        var approvedSchemeCodes = approvedSchemes.Select(x => x.SchemeCode).ToHashSet();
        using var stream = new MemoryStream(excelData);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var rowCount = worksheet.LastRowUsed().RowNumber();

        string currentFundName = string.Empty;
        string currentFundId = string.Empty;

        for (int row = 2; row <= rowCount; row++) // skip header
        {
            try
            {
                var schemeCode = worksheet.Cell(row, 9).GetString().Trim();
                if (string.IsNullOrWhiteSpace(schemeCode)) continue;
                if (!approvedSchemeCodes.Contains(schemeCode)) continue;

                var schemeName = worksheet.Cell(row, 1).GetString().Trim();
                var navText = worksheet.Cell(row, 12).GetString().Trim();
                var dateText = worksheet.Cell(row, 11).GetString().Trim();
                currentFundName = worksheet.Cell(row, 10).GetString().Trim();

                decimal nav = decimal.TryParse(navText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedNav)
                    ? parsedNav : 0;

                DateTime date = DateTime.TryParse(dateText, out var parsedDate) ? parsedDate : DateTime.Now;

                if (AmfiDataHelper.IsFundLine(schemeName))
                {
                    currentFundId = AmfiDataHelper.GenerateFundId(schemeName);
                }

                var existingScheme = await storeContext.SchemeDetails
                    .FirstOrDefaultAsync(x => x.SchemeCode == schemeCode && x.Date == date);

                if (existingScheme == null)
                {
                    await storeContext.SchemeDetails.AddAsync(new SchemeDetail
                    {
                        FundCode = currentFundId,
                        FundName = schemeName,
                        SchemeCode = schemeCode,
                        SchemeName = currentFundName,
                        Nav = nav,
                        Date = date,
                        IsVisible = true
                    });
                }
                else
                {
                    existingScheme.FundCode = currentFundId;
                    existingScheme.Nav = nav;
                    existingScheme.SchemeName = currentFundName;
                    existingScheme.FundName = schemeName;
                }
            }
            catch
            {
                continue; // log if needed
            }
        }

        await storeContext.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message, List<ApprovedData>? Data)> GetSchemesListAsync()
    {
        try
        {
            var result = await storeContext.ApprovedData.ToListAsync();

            if (result == null || result.Count == 0)
            {
                return (false, "No records found.", null);
            }

            return (true, "Records retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred while retrieving records: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<SchemeDetail>? schemeDetails)> GetSchemePerformance(string schemeCode)
    {
        try
        {
            var result = await storeContext.SchemeDetails.Where(x => x.SchemeCode == schemeCode).ToListAsync();

            if (result == null || result.Count == 0)
            {
                return (false, "No records found.", null);
            }

            return (true, "Records retrieved successfully.", result);
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred while retrieving records: {ex.Message}", null);
        }
    }

    public SchemePerformanceResponse TransformToPerformanceResponse(List<SchemeDetail> schemeDetails)
    {
        if (schemeDetails == null || !schemeDetails.Any())
            return null;

        // Sort by date to ensure chronological order
        var sortedDetails = schemeDetails.OrderBy(x => x.Date).ToList();
        var currentDetail = sortedDetails.Last();

        return new SchemePerformanceResponse
        {
            Status = "success",
            SchemeCode = currentDetail.SchemeCode,
            SchemeName = currentDetail.SchemeName,
            FundHouse = currentDetail.FundName,
            CurrentNav = currentDetail.Nav,
            LastUpdated = currentDetail.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Performance = new PerformanceData
            {
                Yesterday = CalculatePerformanceForPeriod(sortedDetails, 1),
                OneWeek = CalculatePerformanceForPeriod(sortedDetails, 7),
                OneMonth = CalculatePerformanceForPeriod(sortedDetails, 30),
                SixMonths = CalculatePerformanceForPeriod(sortedDetails, 180),
                OneYear = CalculatePerformanceForPeriod(sortedDetails, 365)
            },
            HistoricalData = new HistoricalData
            {
                Dates = sortedDetails.Select(r => r.Date.ToString("yyyy-MM-dd")).ToList(),
                NavValues = sortedDetails.Select(r => r.Nav).ToList()
            }
        };
    }

    private NavPerformance CalculatePerformanceForPeriod(List<SchemeDetail> records, int daysBack)
    {
        var current = records.Last();
        var previous = GetBestAvailableRecordForPeriod(records, daysBack);

        return CalculatePerformance(current, previous);
    }

    private SchemeDetail GetBestAvailableRecordForPeriod(List<SchemeDetail> records, int daysBack)
    {
        if (records.Count < 2) return null;

        var currentDate = records.Last().Date;
        var targetDate = currentDate.AddDays(-daysBack);

        // First try to get exact or closest record before target date
        var previousRecord = records
            .Where(r => r.Date <= targetDate)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault();

        // If no record found, use the oldest available record
        if (previousRecord == null)
        {
            previousRecord = records.First();
        }

        return previousRecord;
    }

    private NavPerformance CalculatePerformance(SchemeDetail current, SchemeDetail previous)
    {
        if (previous == null || previous.Date >= current.Date)
            return new NavPerformance { Nav = 0, Date = "", Change = 0, ChangePercentage = 0, IsPositive = false };

        var change = current.Nav - previous.Nav;
        var changePercentage = (change / previous.Nav) * 100;

        return new NavPerformance
        {
            Nav = Math.Round(previous.Nav, 4),
            Date = previous.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Change = Math.Round(change, 4),
            ChangePercentage = Math.Round(changePercentage, 2),
            IsPositive = change >= 0
        };
    }

    public async Task<List<NavRecord>> GetNavRecordsAsync(string schemeCode, DateTime startDate, DateTime endDate)
    {
        return await storeContext.SchemeDetails
            .Where(x => x.SchemeCode == schemeCode &&
                       x.Date >= startDate &&
                       x.Date <= endDate &&
                       x.IsVisible)
            .OrderByDescending(x => x.Date)
            .Select(x => new NavRecord
            {
                Id = x.Id,
                FundHouse = x.FundCode,
                FundName = x.FundName,
                SchemeCode = x.SchemeCode,
                SchemeName = x.SchemeName,
                IsActive = x.IsVisible ? 1 : 0,
                NavDate = x.Date,
                NavValue = x.Nav
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctSchemeCodesAsync()
    {
        return await storeContext.SchemeDetails
            .Where(x => x.IsVisible)
            .Select(x => x.SchemeCode)
            .Distinct()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<NavRecord?> GetLatestNavBeforeDateAsync(string schemeCode, DateTime date)
    {
        return await storeContext.SchemeDetails
            .Where(x => x.SchemeCode == schemeCode &&
                       x.Date <= date &&
                       x.IsVisible)
            .OrderByDescending(x => x.Date)
            .Select(x => new NavRecord
            {
                Id = x.Id,
                FundHouse = x.FundCode,
                FundName = x.FundName,
                SchemeCode = x.SchemeCode,
                SchemeName = x.SchemeName,
                IsActive = x.IsVisible ? 1 : 0,
                NavDate = x.Date,
                NavValue = x.Nav
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<NavHistoryResponse> GetNavHistoryAsync(NavHistoryRequest request)
    {
        try
        {

            var schemeCodes = await GetDistinctSchemeCodesAsync();
            var schemes = new List<SchemeHistory>();
            var allDates = new List<DateTime>();

            // Process each scheme
            foreach (var schemeCode in schemeCodes)
            {
                try
                {
                    // Calculate date range for this scheme based on current date
                    var (startDate, endDate) = CalculateDateRange(request.CurrentDate);

                    // Get all records for the scheme within the date range
                    var schemeRecords = await GetNavRecordsAsync(schemeCode, startDate, endDate);

                    if (schemeRecords.Any())
                    {
                        // Take only the last 2 records for history
                        var lastTwoRecords = schemeRecords
                            .OrderByDescending(x => x.NavDate)
                            .Take(2)
                            .OrderBy(x => x.NavDate)
                            .ToList();

                        var schemeHistory = BuildSchemeHistory(lastTwoRecords, schemeCode);
                        schemes.Add(schemeHistory);

                        // Collect all dates from this scheme's history
                        allDates.AddRange(schemeHistory.History.Select(h => h.Date));

                    }
                    else
                    {
                        //_logger.LogDebug("No records found for scheme {SchemeCode} in the date range", schemeCode);
                    }
                }
                catch (Exception ex)
                {
                    // Continue with other schemes even if one fails
                }
            }

            // Calculate actual start and end dates from all scheme data
            var (actualStartDate, actualEndDate) = CalculateActualDateRange(allDates);

            // Apply ranking logic
            var rankedSchemes = ApplyRanking(schemes);


            return new NavHistoryResponse
            {
                StartDate = actualStartDate,
                EndDate = actualEndDate,
                Message = $"Retrieved {rankedSchemes.Count} schemes successfully.",
                Schemes = rankedSchemes
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private (DateTime startDate, DateTime endDate) CalculateActualDateRange(List<DateTime> allDates)
    {
        if (!allDates.Any())
        {
            // If no dates found, return default range
            var defaultEndDate = DateTime.Today.AddDays(-1);
            var defaultStartDate = defaultEndDate.AddDays(-1);
            return (defaultStartDate, defaultEndDate);
        }

        var minDate = allDates.Min();
        var maxDate = allDates.Max();
        return (minDate, maxDate);
    }

    private List<SchemeHistory> ApplyRanking(List<SchemeHistory> schemes)
    {
        if (!schemes.Any())
            return schemes;

        var schemesWithRank = schemes
            .Select(s => new
            {
                Scheme = s,
                LatestPercentage = s.History
                    .OrderByDescending(h => h.Date)
                    .Select(h => decimal.TryParse(h.Percentage, out var pct) ? pct : 0m)
                    .FirstOrDefault()
            })
            .OrderByDescending(s => s.LatestPercentage)
            .ThenBy(s => s.Scheme.FundName) // ensure stable ordering
            .Select((s, index) =>
            {
                // Rank logic: top 3 = 1,2,3; all others = 4
                s.Scheme.Rank = index < 3 ? index + 1 : 4;
                return s.Scheme;
            })
            .OrderBy(s => s.Rank) // Final rank-based ordering
            .ToList();

        return schemesWithRank;
    }

    private SchemeHistory BuildSchemeHistory(List<NavRecord> records, string schemeCode)
    {
        var history = new List<NavHistory>();
        var firstRecord = records.First();

        if (records.Count == 0)
        {
            return new SchemeHistory
            {
                FundName = "",
                SchemeCode = schemeCode,
                SchemeName = "",
                History = history,
                Rank = null
            };
        }

        // If we have only one record, we need to create a synthetic previous entry
        if (records.Count == 1)
        {
            var singleRecord = records.First();

            // Try to find the previous record
            var previousDate = singleRecord.NavDate.AddDays(-1);
            var previousRecord = GetLatestNavBeforeDateAsync(schemeCode, previousDate).Result;

            if (previousRecord != null)
            {
                // Use actual previous record if available
                var navHistory1 = new NavHistory
                {
                    Date = previousRecord.NavDate,
                    Nav = previousRecord.NavValue,
                    Percentage = "100",
                    IsTradingHoliday = false,
                    IsGrowth = false
                };

                var percentageChange = CalculatePercentageChange(previousRecord.NavValue, singleRecord.NavValue);
                var navHistory2 = new NavHistory
                {
                    Date = singleRecord.NavDate,
                    Nav = singleRecord.NavValue,
                    Percentage = percentageChange.ToString("F2"),
                    IsTradingHoliday = false,
                    IsGrowth = percentageChange > 0
                };

                history.Add(navHistory1);
                history.Add(navHistory2);
            }
            else
            {
                // Create synthetic previous entry
                var navHistory1 = new NavHistory
                {
                    Date = singleRecord.NavDate.AddDays(-1),
                    Nav = singleRecord.NavValue,
                    Percentage = "100",
                    IsTradingHoliday = false,
                    IsGrowth = false
                };

                var navHistory2 = new NavHistory
                {
                    Date = singleRecord.NavDate,
                    Nav = singleRecord.NavValue,
                    Percentage = "0.00",
                    IsTradingHoliday = false,
                    IsGrowth = false
                };

                history.Add(navHistory1);
                history.Add(navHistory2);
            }
        }
        else if (records.Count >= 2)
        {
            // We have at least 2 records, use the actual data
            var firstNavRecord = records[0]; // Older date
            var secondNavRecord = records[1]; // Newer date

            // First record (older date)
            var navHistory1 = new NavHistory
            {
                Date = firstNavRecord.NavDate,
                Nav = firstNavRecord.NavValue,
                Percentage = "100",
                IsTradingHoliday = false,
                IsGrowth = false
            };

            // Second record (newer date) - calculate percentage change
            var percentageChange = CalculatePercentageChange(firstNavRecord.NavValue, secondNavRecord.NavValue);
            var navHistory2 = new NavHistory
            {
                Date = secondNavRecord.NavDate,
                Nav = secondNavRecord.NavValue,
                Percentage = percentageChange.ToString("F2"),
                IsTradingHoliday = false,
                IsGrowth = percentageChange > 0
            };

            history.Add(navHistory1);
            history.Add(navHistory2);
        }

        return new SchemeHistory
        {
            FundName = firstRecord.FundName,
            SchemeCode = firstRecord.SchemeCode,
            SchemeName = firstRecord.SchemeName,
            History = history,
            Rank = null // Rank will be set later in ApplyRanking method
        };
    }

    private decimal CalculatePercentageChange(decimal baseNav, decimal currentNav)
    {
        if (baseNav == 0) return 0;
        return ((currentNav - baseNav) / baseNav) * 100;
    }

    private (DateTime startDate, DateTime endDate) CalculateDateRange(DateTime currentDate)
    {
        // For the given current date, we want to get the last 2 trading days
        var endDate = GetLastTradingDate(currentDate);
        var startDate = GetLastTradingDate(endDate.AddDays(-7)); // Look back 7 days to ensure we find the previous trading day

        return (startDate, endDate);
    }

    private DateTime GetLastTradingDate(DateTime fromDate)
    {
        var date = fromDate;
        // Go back until we find a weekday (Monday-Friday)
        // In real scenario, you might want to check for trading holidays here
        while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(-1);
        }
        return date;
    }
}