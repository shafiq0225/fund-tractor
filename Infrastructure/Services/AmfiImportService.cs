using System;
using Core.Entities.AMFI;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AmfiImportService(StoreContext storeContext)
{
    private readonly StoreContext storeContext = storeContext;

    public void ImportAmfiData(string rawData)
    {
        var lines = rawData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        string currentFundName = "";
        string currentFundId = "";
        int fundIndex = 1;

        foreach (var line in lines)
        {
            if (line.StartsWith("Scheme Code") || line.Contains("Open Ended"))
                continue;

            if (string.IsNullOrWhiteSpace(line)) continue;

            // Detect fund (line without ;)
            if (!line.Contains(";"))
            {
                currentFundName = line.Trim();
                currentFundId = GenerateFundId(currentFundName, fundIndex++);

                if (!storeContext.Funds.Any(f => f.FundId == currentFundId))
                {
                    storeContext.Funds.Add(new Fund
                    {
                        FundId = currentFundId,
                        FundName = currentFundName,
                        IsManagerApproved = false,
                        IsVisible = false,
                        ApprovedBy = "Shafiq"
                    });
                    storeContext.SaveChanges();
                }
                continue;
            }

            // Parse CSV line
            var parts = line.Split(';');
            if (parts.Length < 6) continue;

            var schemeCode = parts[0].Trim();
            var schemeName = parts[3].Trim();
            if (!decimal.TryParse(parts[4], out var nav)) nav = 0;
            if (!DateTime.TryParse(parts[5], out var date)) date = DateTime.UtcNow;

            // Check if scheme already exists
            if (!storeContext.Schemes.Any(s => s.SchemeId == schemeCode))
            {
                var scheme = new Scheme
                {
                    SchemeId = schemeCode,
                    FundId = currentFundId,
                    SchemeName = schemeName,
                    IsManagerApproved = false,
                    IsVisible = false,
                    ApprovedBy = "Shafiq"
                };
                storeContext.Schemes.Add(scheme);
            }
            var amfi = storeContext.AmfiRawDatas
                .FirstOrDefault(a => a.SchemeCode == schemeCode);

            bool isVisible = amfi?.IsVisible ?? false;
            // Always store raw data (full history)
            storeContext.AmfiRawDatas.Add(new AmfiRawData
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

        storeContext.SaveChanges();
    }

    private static string GenerateFundId(string fundName, int index)
    {
        var normalized = new string(fundName.Where(char.IsLetterOrDigit).ToArray());
        return $"{normalized}MF_{index}";
    }
    public void SetFundApproval(string fundId, bool isApproved)
    {
        var fund = storeContext.Funds
            .Include(f => f.Schemes)
            .FirstOrDefault(f => f.FundId == fundId);

        if (fund == null) return;

        fund.IsManagerApproved = isApproved;
        fund.IsVisible = isApproved;

        foreach (var scheme in fund.Schemes)
        {
            scheme.IsManagerApproved = isApproved;
            scheme.IsVisible = isApproved;
        }

        var amfiData = storeContext.AmfiRawDatas.Where(x => x.FundId == fundId && x.IsVisible != true);

        foreach (var af in amfiData)
        {
            af.IsVisible = isApproved;
        }

        storeContext.SaveChanges();
    }

    public void SetSchemeApproval(string schemeId, bool isApproved)
    {
        var scheme = storeContext.Schemes.FirstOrDefault(s => s.SchemeId == schemeId);
        if (scheme == null) return;

        var fund = storeContext.Funds
            .Include(f => f.Schemes)
            .FirstOrDefault(f => f.FundId == scheme.FundId);

        if (fund == null) return;

        // update scheme
        scheme.IsManagerApproved = isApproved;
        scheme.IsVisible = isApproved;

        // if approving a scheme → ensure fund is also approved
        if (isApproved)
        {
            fund.IsManagerApproved = true;
            fund.IsVisible = true;
        }
        var amfiData = storeContext.AmfiRawDatas.FirstOrDefault(x => x.SchemeCode == schemeId && x.IsVisible != true);
        if (amfiData != null)
        {
            amfiData.IsVisible = isApproved;
        }
        // if unapproving a scheme, we don’t auto-unapprove the fund 
        // (fund might still be visible if other schemes are approved)
        // unless you want ALL child unapproval to hide the fund,
        // then add a check like:
        // if (fund.Schemes.All(s => !s.IsManagerApproved))
        // {
        //     fund.IsManagerApproved = false;
        //     fund.IsVisible = false;
        // }

        storeContext.SaveChanges();
    }


}
