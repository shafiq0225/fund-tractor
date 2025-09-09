using Core.DTOs;
using Core.Entities.AMFI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Helpers
{
    public static class SchemeTransformer
    {
        public static List<SchemeDto> BuildSchemeHistory(List<SchemeDetail> schemes, List<DateTime> allDates, DateTime startDate, DateTime endDate)
        {
            var result = new List<SchemeDto>();

            // Group schemes by code, fund, and name
            var groupedSchemes = schemes.GroupBy(s => new { s.SchemeCode, s.FundName, s.SchemeName });

            foreach (var group in groupedSchemes)
            {
                var schemeDto = new SchemeDto
                {
                    FundName = group.Key.FundName,
                    SchemeCode = group.Key.SchemeCode,
                    SchemeName = group.Key.SchemeName,
                    History = new List<SchemeHistoryDto>()
                };

                // Build history for each date, skipping start and end dates
                foreach (var date in allDates)
                {
                    if (date.Date == startDate.Date || date.Date == endDate.Date)
                        continue; // skip start/end date

                    var currentRecord = group.FirstOrDefault(r => r.Date.Date == date.Date);

                    decimal? percentChange = null;
                    string percentDisplay = null;
                    bool? isGrowth = null;
                    decimal? nav = null;

                    if (currentRecord != null)
                    {
                        nav = Math.Round(currentRecord.Nav, 4);
                        var prevDate = date.AddDays(-1);
                        var prevRecord = group.FirstOrDefault(r => r.Date.Date == prevDate.Date);

                        if (prevRecord != null && prevRecord.Nav > 0)
                        {
                            percentChange = ((currentRecord.Nav - prevRecord.Nav) / prevRecord.Nav) * 100;
                            percentChange = Math.Round(percentChange.Value, 2);
                            percentDisplay = $"{percentChange.Value:0.00}%";

                            isGrowth = currentRecord.Nav > prevRecord.Nav;
                        }
                    }

                    schemeDto.History.Add(new SchemeHistoryDto
                    {
                        Date = date,
                        Nav = nav,
                        IsGrowth = isGrowth ?? false,
                        Percentage = percentDisplay,
                        IsTradingHoliday = currentRecord == null
                    });
                }

                result.Add(schemeDto);
            }

            return result;
        }
    }

}

