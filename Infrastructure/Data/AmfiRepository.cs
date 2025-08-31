using System;
using Core.Entities.AMFI;
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
        int fundIndex = 1;

        foreach (var line in lines)
        {
            if (IsHeaderOrSection(line))
                continue;

            if (IsFundLine(line))
            {
                (currentFundName, currentFundId) = await ProcessFundAsync(line.Trim(), fundIndex++);
                continue;
            }

            await ProcessRawDataAsync(line, currentFundId, currentFundName);
        }

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

    private static bool IsHeaderOrSection(string line)
    {
        return line.StartsWith("Scheme Code") || line.Contains("Open Ended") || string.IsNullOrWhiteSpace(line);
    }

    private static bool IsFundLine(string line)
    {
        return !line.Contains(";");
    }

    private async Task<(string fundName, string fundId)> ProcessFundAsync(string fundName, int index)
    {
        var fundId = GenerateFundId(fundName, index);

        bool exists = await storeContext.Funds.AnyAsync(f => f.FundId == fundId);
        if (!exists)
        {
            await storeContext.Funds.AddAsync(new Fund
            {
                FundId = fundId,
                FundName = fundName,
                IsManagerApproved = false,
                IsVisible = false,
                ApprovedBy = "Shafiq"
            });
            await SaveChangesAsync();
        }

        return (fundName, fundId);
    }

    private static string GenerateFundId(string fundName, int index)
    {
        var normalized = new string(fundName.Where(char.IsLetterOrDigit).ToArray());
        return $"{normalized}MF_{index}";
    }

    private async Task ProcessRawDataAsync(string line, string currentFundId, string currentFundName)
    {
        var parts = line.Split(';');
        if (parts.Length < 6) return;

        var schemeCode = parts[0].Trim();
        var schemeName = parts[3].Trim();

        decimal nav = decimal.TryParse(parts[4], out var parsedNav) ? parsedNav : 0;
        DateTime date = DateTime.TryParse(parts[5], out var parsedDate) ? parsedDate : DateTime.UtcNow;

        await EnsureSchemeExistsAsync(schemeCode, schemeName, currentFundId);

        var amfi = await storeContext.AmfiRawDatas
            .FirstOrDefaultAsync(a => a.SchemeCode == schemeCode);

        bool isVisible = amfi?.IsVisible ?? false;

        await storeContext.AmfiRawDatas.AddAsync(new AmfiRawData
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

    private async Task EnsureSchemeExistsAsync(string schemeCode, string schemeName, string fundId)
    {
        bool exists = await storeContext.Schemes.AnyAsync(s => s.SchemeId == schemeCode);
        if (exists) return;

        await storeContext.Schemes.AddAsync(new Scheme
        {
            SchemeId = schemeCode,
            FundId = fundId,
            SchemeName = schemeName,
            IsManagerApproved = false,
            IsVisible = false,
            ApprovedBy = "Shafiq"
        });
    }

}
