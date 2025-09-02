using System;
using Core.Entities.AMFI;
using Core.Helpers;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AmfiRepository(StoreContext storeContext) : IAmfiRepository
{
    public async Task ImportAmfiDataAsync(string rawData)
    {
        var lines = rawData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        string currentFundName = string.Empty;
        string currentFundId = string.Empty;

        // Cache to avoid duplicate DB queries
        var existingFunds = (await storeContext.Funds.Select(f => f.FundId).ToListAsync()).ToHashSet();

        var existingSchemes = (await storeContext.Schemes.Select(s => s.SchemeId).ToListAsync()).ToHashSet();

        var existingAmfiData = (await storeContext.AmfiRawDatas.Select(a => a.SchemeCode).ToListAsync()).ToHashSet();

        var newFunds = new List<Fund>();
        var newSchemes = new List<Scheme>();
        var newAmfiData = new List<AmfiRawData>();

        foreach (var line in lines)
        {
            if (AmfiDataHelper.IsHeaderOrSection(line))
                continue;

            if (AmfiDataHelper.IsFundLine(line))
            {
                currentFundName = line.Trim();
                currentFundId = AmfiDataHelper.GenerateFundId(currentFundName);

                if (!existingFunds.Contains(currentFundId))
                {
                    newFunds.Add(new Fund
                    {
                        FundId = currentFundId,
                        FundName = currentFundName,
                        IsManagerApproved = false,
                        IsVisible = false,
                        ApprovedBy = "Shafiq"
                    });
                    existingFunds.Add(currentFundId); // Update cache
                }

                continue;
            }

            var parts = line.Split(';');
            if (parts.Length < 6) continue;

            var schemeCode = parts[0].Trim();
            var schemeName = parts[3].Trim();

            decimal nav = decimal.TryParse(parts[4], out var parsedNav) ? parsedNav : 0;
            DateTime date = DateTime.TryParse(parts[5], out var parsedDate) ? parsedDate : DateTime.UtcNow;

            if (!existingSchemes.Contains(schemeCode))
            {
                newSchemes.Add(new Scheme
                {
                    SchemeId = schemeCode,
                    FundId = currentFundId,
                    SchemeName = schemeName,
                    IsManagerApproved = false,
                    IsVisible = false,
                    ApprovedBy = "Shafiq"
                });
                existingSchemes.Add(schemeCode); // Update cache
            }

            bool isVisible = existingAmfiData.Contains(schemeCode)
                ? (await storeContext.AmfiRawDatas
                        .Where(a => a.SchemeCode == schemeCode)
                        .Select(a => a.IsVisible)
                        .FirstOrDefaultAsync())
                : false;

            newAmfiData.Add(new AmfiRawData
            {
                FundId = currentFundId,
                FundName = currentFundName,
                SchemeCode = schemeCode,
                SchemeName = schemeName,
                NetAssetValue = nav,
                Date = date,
                IsVisible = isVisible
            });
        }

        // Perform batched inserts
        if (newFunds.Any()) await storeContext.Funds.AddRangeAsync(newFunds);
        if (newSchemes.Any()) await storeContext.Schemes.AddRangeAsync(newSchemes);
        if (newAmfiData.Any()) await storeContext.AmfiRawDatas.AddRangeAsync(newAmfiData);

        await SaveChangesAsync();
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await storeContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> SetFundApprovalAsync(string fundId, bool isApproved)
    {
        var fund = await storeContext.Funds
            .Include(f => f.Schemes)
            .FirstOrDefaultAsync(f => f.FundId == fundId);

        if (fund is null)
            return false;

        // Update Fund
        fund.IsManagerApproved = isApproved;
        fund.IsVisible = isApproved;

        // Update related Schemes
        foreach (var scheme in fund.Schemes)
        {
            scheme.IsManagerApproved = isApproved;
            scheme.IsVisible = isApproved;
        }

        // Update AmfiRawData records
        var amfiRecords = await storeContext.AmfiRawDatas
            .Where(x => x.FundId == fundId)
            .ToListAsync();

        foreach (var record in amfiRecords)
        {
            record.IsVisible = isApproved;
        }

        await SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetSchemeApprovalAsync(string schemeId, bool isApproved)
    {
        // Fetch scheme along with its parent fund in a single query
        var scheme = await storeContext.Schemes
            .Include(s => s.Fund)
            .FirstOrDefaultAsync(s => s.SchemeId == schemeId);

        if (scheme == null) return false;

        // Update scheme approval and visibility
        scheme.IsManagerApproved = isApproved;
        scheme.IsVisible = isApproved;

        // Update fund approval/visibility if approving a scheme
        if (scheme.Fund != null)
        {
            scheme.Fund.IsManagerApproved = isApproved;
            scheme.Fund.IsVisible = isApproved;
        }

        // Update AMFI raw data visibility if needed
        var amfiDatas = await storeContext.AmfiRawDatas.Where(x => x.SchemeCode == schemeId).ToListAsync();


        if (amfiDatas.Count != 0)
        {
            foreach (var amfiData in amfiDatas)
            {
                amfiData.IsVisible = isApproved;
            }
        }

        await SaveChangesAsync();
        return true;
    }

}
