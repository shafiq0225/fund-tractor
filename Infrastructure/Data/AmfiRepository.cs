using System.Globalization;
using ClosedXML.Excel;
using Core.DTOs;
using Core.Entities.AMFI;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AmfiRepository(StoreContext storeContext, IUserService _userService) : IAmfiRepository
{
    public async Task<List<ApprovedData>> GetApprovedSchemesAsync()
        => await storeContext.ApprovedData.Where(x => x.IsApproved).ToListAsync();

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
            // Get current user's name
            var approvedByName = await _userService.GetCurrentUserNameAsync();
            if (string.IsNullOrEmpty(approvedByName))
            {
                return (false, "Unable to determine current user.");
            }
            // ✅ Check if record already exists
            var exists = await storeContext.ApprovedData
                .AnyAsync(x => x.FundCode == fundId && x.SchemeCode == addSchemeDto.SchemeId);

            if (exists)
                return (false, "Record already exists.");

            var approvedFund = new ApprovedData
            {
                ApprovedName = approvedByName,
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

            // Get current user's name
            var approvedByName = await _userService.GetCurrentUserNameAsync();
            if (string.IsNullOrEmpty(approvedByName))
            {
                return (false, "Unable to determine current user.");
            }

            // ✅ Update ApprovedData
            existingRecord.IsApproved = isApproved;
            existingRecord.LastUpdatedDate = DateTime.UtcNow;
            existingRecord.ApprovedName = approvedByName;
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

            // Get current user's name
            var approvedByName = await _userService.GetCurrentUserNameAsync();
            if (string.IsNullOrEmpty(approvedByName))
            {
                return (false, "Unable to determine current user.");
            }
            foreach (var record in existingRecords)
            {
                record.IsApproved = isApproved;
                record.LastUpdatedDate = DateTime.UtcNow;
                record.ApprovedName = approvedByName;
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

    //public async Task<(bool Success, string Message, List<SchemeDetail>? Data)> GetSchemesByDateRangeAsync(DateTime startDate, DateTime endDate)
    //{
    //    if (startDate > endDate)
    //    {
    //        return (false, "Start date cannot be later than end date.", null);
    //    }

    //    try
    //    {
    //        var result = await storeContext.SchemeDetails
    //            .Where(x => x.Date >= startDate && x.Date <= endDate && x.IsVisible)
    //            .OrderBy(x => x.Date)
    //            .ToListAsync();

    //        if (result.Count == 0)
    //            return (false, "No records found for the given date range.", new List<SchemeDetail>());

    //        return (true, "Records retrieved successfully.", result);
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        return (false, "The request was canceled before completion.", null);
    //    }
    //    catch (DbUpdateException dbEx)
    //    {
    //        return (false, $"Database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}", null);
    //    }
    //    catch (Exception ex)
    //    {
    //        return (false, $"Unexpected error: {ex.Message}", null);
    //    }
    //}

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
                    // Get the last 3 records to calculate percentages properly
                    var schemeRecords = await GetLastThreeNavRecordsAsync(schemeCode, request.CurrentDate);

                    if (schemeRecords.Count >= 2)
                    {
                        var schemeHistory = BuildSchemeHistoryWithIndividualPercentages(schemeRecords, schemeCode);
                        schemes.Add(schemeHistory);

                        // Collect all dates from this scheme's history
                        allDates.AddRange(schemeHistory.History.Select(h => h.Date));
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

    private async Task<List<NavRecord>> GetLastThreeNavRecordsAsync(string schemeCode, DateTime currentDate)
    {
        var (startDate, endDate) = CalculateDateRange(currentDate);

        // Get more records to ensure we have data for percentage calculation
        var allRecords = await GetNavRecordsAsync(schemeCode, startDate.AddDays(-5), endDate);

        // Take the last 3 records
        return allRecords
            .OrderByDescending(x => x.NavDate)
            .Take(3)
            .OrderBy(x => x.NavDate)
            .ToList();
    }

    private SchemeHistory BuildSchemeHistoryWithIndividualPercentages(List<NavRecord> records, string schemeCode)
    {
        var history = new List<NavHistory>();
        var firstRecord = records.First();

        // We need at least 2 records
        if (records.Count < 2)
        {
            return BuildFallbackSchemeHistory(records, schemeCode);
        }

        // The records are ordered by date (oldest first)
        NavRecord baseRecord = null;
        NavRecord firstHistoryRecord = null;
        NavRecord secondHistoryRecord = null;

        if (records.Count >= 3)
        {
            // We have: [base], [first history], [second history]
            baseRecord = records[0];
            firstHistoryRecord = records[1];
            secondHistoryRecord = records[2];
        }
        else if (records.Count == 2)
        {
            // We only have two records, use the older one as base for both
            baseRecord = records[0];
            firstHistoryRecord = records[0]; // Same as base
            secondHistoryRecord = records[1];
        }

        // Calculate percentages from base record
        var firstPercentage = CalculatePercentageChange(baseRecord.NavValue, firstHistoryRecord.NavValue);
        var secondPercentage = CalculatePercentageChange(firstHistoryRecord.NavValue, secondHistoryRecord.NavValue);

        // Build history entries - BOTH show actual percentages, NO "100"
        var navHistory1 = new NavHistory
        {
            Date = firstHistoryRecord.NavDate,
            Nav = firstHistoryRecord.NavValue,
            Percentage = firstPercentage.ToString("F2"), // Show actual percentage
            IsTradingHoliday = false,
            IsGrowth = firstPercentage > 0
        };

        var navHistory2 = new NavHistory
        {
            Date = secondHistoryRecord.NavDate,
            Nav = secondHistoryRecord.NavValue,
            Percentage = secondPercentage.ToString("F2"), // Show actual percentage
            IsTradingHoliday = false,
            IsGrowth = secondPercentage > 0
        };

        history.Add(navHistory1);
        history.Add(navHistory2);

        return new SchemeHistory
        {
            FundName = firstRecord.FundName,
            SchemeCode = firstRecord.SchemeCode,
            SchemeName = firstRecord.SchemeName,
            History = history,
            Rank = null
        };
    }

    private SchemeHistory BuildFallbackSchemeHistory(List<NavRecord> records, string schemeCode)
    {
        var history = new List<NavHistory>();
        var firstRecord = records.First();

        if (records.Count == 1)
        {
            // Single record - both entries show same data with 0% change
            var singleRecord = records.First();

            var navHistory1 = new NavHistory
            {
                Date = singleRecord.NavDate.AddDays(-1),
                Nav = singleRecord.NavValue,
                Percentage = "0.00", // No "100"
                IsTradingHoliday = false,
                IsGrowth = false
            };

            var navHistory2 = new NavHistory
            {
                Date = singleRecord.NavDate,
                Nav = singleRecord.NavValue,
                Percentage = "0.00", // No "100"
                IsTradingHoliday = false,
                IsGrowth = false
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
            Rank = null
        };
    }

    private decimal CalculatePercentageChange(decimal baseNav, decimal currentNav)
    {
        if (baseNav == 0) return 0;
        return ((currentNav - baseNav) / baseNav) * 100;
    }

    private (DateTime startDate, DateTime endDate) CalculateDateRange(DateTime currentDate)
    {
        var endDate = GetLastTradingDate(currentDate);
        var startDate = GetLastTradingDate(endDate.AddDays(-7));
        return (startDate, endDate);
    }

    private DateTime GetLastTradingDate(DateTime fromDate)
    {
        var date = fromDate;
        while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(-1);
        }
        return date;
    }

    private (DateTime startDate, DateTime endDate) CalculateActualDateRange(List<DateTime> allDates)
    {
        if (!allDates.Any())
        {
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
                // Use the second history entry's percentage for ranking
                LatestPercentage = s.History
                    .OrderByDescending(h => h.Date)
                    .Select(h => decimal.TryParse(h.Percentage, out var pct) ? pct : 0m)
                    .FirstOrDefault()
            })
            .OrderByDescending(s => s.LatestPercentage)
            .ThenBy(s => s.Scheme.FundName)
            .Select((s, index) =>
            {
                s.Scheme.Rank = index < 3 ? index + 1 : 4;
                return s.Scheme;
            })
            .OrderBy(s => s.Rank)
            .ToList();

        return schemesWithRank;
    }

    public async Task<FundDataResponse> GetFundsBySchemeCodes(List<string> schemeCodes)
    {
        var response = new FundDataResponse();
        var allFunds = new Dictionary<string, FundResponse>();

        foreach (var schemeCode in schemeCodes)
        {
            var fundData = await GetFundBySchemeCode(schemeCode);
            foreach (var fund in fundData)
            {
                allFunds[fund.Key] = fund.Value;
            }
        }

        // Calculate ranks based on 1-year returns
        CalculateRanksBasedOn1YearReturns(allFunds);

        foreach (var fund in allFunds.OrderBy(r=> r.Value.CrisilRank))
        {
            response[fund.Key] = fund.Value;
        }

        return response;
    }

    public async Task<FundDataResponse> GetFundBySchemeCode(string schemeCode)
    {
        var navData = await storeContext.SchemeDetails
            .Where(n => n.SchemeCode == schemeCode && n.IsVisible)
            .OrderByDescending(n => n.Date)
            .Take(365)
            .ToListAsync();

        if (!navData.Any())
        {
            return new FundDataResponse();
        }

        var fundName = navData.First().SchemeName;
        var response = new FundDataResponse
        {
            [fundName] = CalculateFundResponse(navData, fundName)
        };

        return response;
    }

    private FundResponse CalculateFundResponse(List<SchemeDetail> navData, string fundName)
    {
        var sortedData = navData.OrderBy(n => n.Date).ToList();

        var returns = new FundReturns
        {
            Yesterday = Math.Round(CalculateReturn(sortedData, 1), 2),
            _1week = Math.Round(CalculateReturn(sortedData, 7), 2),
            _1m = Math.Round(CalculateReturn(sortedData, 30), 2),
            _6m = Math.Round(CalculateReturn(sortedData, 180), 2),
            _1y = Math.Round(CalculateReturn(sortedData, 365), 2)
        };

        var monthlyReturns = CalculateMonthlyReturns(sortedData);

        return new FundResponse
        {
            Name = fundName,
            CrisilRank = 0, // Temporary - will be calculated later
            Returns = returns,
            MonthlyReturns = monthlyReturns
        };
    }

    private void CalculateRanksBasedOn1YearReturns(Dictionary<string, FundResponse> allFunds)
    {
        var fundsWithReturns = allFunds
            .Select(kvp => new
            {
                FundName = kvp.Key,
                OneYearReturn = kvp.Value.Returns._1y,
                FundResponse = kvp.Value
            })
            .ToList();

        var sortedFunds = fundsWithReturns.OrderByDescending(f => f.OneYearReturn).ToList();

        for (int i = 0; i < sortedFunds.Count; i++)
        {
            sortedFunds[i].FundResponse.CrisilRank = i + 1;
        }
    }

    private decimal CalculateReturn(List<SchemeDetail> sortedData, int daysBack)
    {
        var latestDate = sortedData.Last().Date;
        var targetDate = latestDate.AddDays(-daysBack);

        var latestNav = sortedData.Last().Nav;
        var historicalNav = sortedData
            .Where(n => n.Date <= targetDate)
            .OrderByDescending(n => n.Date)
            .FirstOrDefault()?.Nav;

        if (historicalNav == null)
        {
            historicalNav = sortedData
                .OrderBy(n => n.Date)
                .FirstOrDefault()?.Nav;
        }

        if (historicalNav == null || historicalNav == 0 || latestNav == 0)
            return 0;

        var returnValue = ((latestNav - historicalNav.Value) / historicalNav.Value) * 100;
        return Math.Round(returnValue, 4); // Keep more precision for intermediate calculations
    }
    private List<decimal> CalculateMonthlyReturns(List<SchemeDetail> sortedData)
    {
        var monthlyReturns = new List<decimal>();
        var currentDate = sortedData.Last().Date;

        for (int i = 0; i < 12; i++)
        {
            var monthStart = currentDate.AddMonths(-i).Date;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1).Date;

            // Find start NAV - use oldest available in the first week
            var startNav = sortedData
                .Where(n => n.Date >= monthStart && n.Date <= monthStart.AddDays(7))
                .OrderBy(n => n.Date)
                .FirstOrDefault()?.Nav;

            // If no record found in first week, use the closest available record before month end
            if (startNav == null)
            {
                startNav = sortedData
                    .Where(n => n.Date <= monthStart)
                    .OrderByDescending(n => n.Date)
                    .FirstOrDefault()?.Nav;
            }

            // Find end NAV - use newest available in the last week
            var endNav = sortedData
                .Where(n => n.Date >= monthEnd.AddDays(-7) && n.Date <= monthEnd)
                .OrderByDescending(n => n.Date)
                .FirstOrDefault()?.Nav;

            // If no record found in last week, use the closest available record before month end
            if (endNav == null)
            {
                endNav = sortedData
                    .Where(n => n.Date <= monthEnd)
                    .OrderByDescending(n => n.Date)
                    .FirstOrDefault()?.Nav;
            }

            if (startNav != null && endNav != null && startNav > 0)
            {
                var monthlyReturn = ((endNav.Value - startNav.Value) / startNav.Value) * 100;
                monthlyReturns.Add(Math.Round(monthlyReturn, 2));
            }
            else
            {
                monthlyReturns.Add(0);
            }
        }

        monthlyReturns.Reverse(); // Order from oldest to newest
        return monthlyReturns;
    }
}