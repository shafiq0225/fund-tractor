using System;
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
        var approvedSchemes = await GetApprovedSchemesAsync();
        var approvedSchemeCodes = approvedSchemes.Select(x => x.SchemeCode).ToHashSet();

        var lines = rawData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var schemes = new List<SchemeDetail>();
        string currentFundName = string.Empty;
        string currentFundId = string.Empty;

        foreach (var line in lines)
        {
            if (line.StartsWith("Scheme Code") || line.StartsWith("Open Ended") || line.StartsWith(" "))
                continue; // skip headers

            if (AmfiDataHelper.IsFundLine(line))
            {
                currentFundName = line.Trim();
                currentFundId = AmfiDataHelper.GenerateFundId(currentFundName);
            }
            var parts = line.Split(';');
            if (parts.Length < 6) continue;

            var schemeCode = parts[0].Trim();
            if (!approvedSchemeCodes.Contains(schemeCode)) continue; // save only approved

            var schemeName = parts[3].Trim();
            var nav = decimal.TryParse(parts[4], out var parsedNav) ? parsedNav : 0;
            var date = DateTime.TryParse(parts[5], out var parsedDate) ? parsedDate : DateTime.Now;

            var existingScheme = await storeContext.SchemeDetails
           .FirstOrDefaultAsync(x => x.SchemeCode == schemeCode && x.Date == parsedDate);

            if (existingScheme == null)
            {
                // Insert new
                var newScheme = new SchemeDetail
                {
                    FundCode = currentFundId,
                    FundName = currentFundName,
                    SchemeCode = schemeCode,
                    SchemeName = schemeName,
                    Nav = nav,
                    Date = parsedDate,
                    IsVisible = true
                };

                await storeContext.SchemeDetails.AddAsync(newScheme);
            }
            else
            {
                // Update NAV if changed
                existingScheme.Nav = nav;
                existingScheme.SchemeName = schemeName; // optional, if scheme name changes
                existingScheme.FundName = currentFundName; // optional sync
            }

        }

        await storeContext.SaveChangesAsync();

    }

    public async Task<(bool Success, string Message)> AddApprovedSchemeAsync(string fundName, string schemeId, bool isApproved)
    {
        var fundId = AmfiDataHelper.GenerateFundId(fundName);

        // ✅ Check if record already exists
        var exists = await storeContext.ApprovedData
            .AnyAsync(x => x.FundCode == fundId && x.SchemeCode == schemeId);

        if (exists)
            return (false, "Already exists");

        var approvedFund = new ApprovedData
        {
            ApprovedName = "Shafiq",
            FundCode = fundId,
            IsApproved = isApproved,
            SchemeCode = schemeId,
        };

        await storeContext.ApprovedData.AddAsync(approvedFund);
        await storeContext.SaveChangesAsync();

        return (true, "Inserted successfully");
    }

    public async Task<(bool Success, string Message)> UpdateApprovedSchemeAsync(string fundId, string schemeId, bool isApproved)
    {
        var existingRecord = await storeContext.ApprovedData
            .FirstOrDefaultAsync(x => x.FundCode == fundId && x.SchemeCode == schemeId);

        if (existingRecord == null)
            return (false, "Record not found");

        if (existingRecord.IsApproved == isApproved)
            return (false, "No changes made");

        existingRecord.IsApproved = isApproved;
        storeContext.ApprovedData.Update(existingRecord);

        var schemaDetails = await storeContext.SchemeDetails.FirstOrDefaultAsync(x => x.FundCode == fundId && x.SchemeCode == schemeId) ?? new SchemeDetail();
        schemaDetails.IsVisible = isApproved;

        await storeContext.SaveChangesAsync();

        return (true, "Updated successfully");
    }

    public async Task<(bool Success, string Message)> UpdateApprovedFundAsync(string fundId, bool isApproved)
    {
        var existingRecords = await storeContext.ApprovedData
            .Where(x => x.FundCode == fundId)
            .ToListAsync();

        if (existingRecords.Count == 0)
            return (false, "Record not found");

        foreach (var record in existingRecords)
        {
            record.IsApproved = isApproved;
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

    public async Task<List<SchemeDetail>> GetSchemesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await storeContext.SchemeDetails
            .Where(x => x.Date >= startDate && x.Date <= endDate && x.IsVisible)
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

}
