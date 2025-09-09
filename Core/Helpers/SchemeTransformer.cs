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
        public static TransformedResult TransformSchemes(List<SchemeDetail> schemes)
        {
            var transformedSchemes = new List<TransformedScheme>();

            var groupedSchemes = schemes.GroupBy(s => s.SchemeName);

            foreach (var group in groupedSchemes)
            {
                var lastThree = group.OrderBy(s => s.Date).TakeLast(3).ToList();
                if (lastThree.Count < 3)
                    continue; // skip if not enough NAVs

                var beforePrevious = lastThree[0];
                var previous = lastThree[1];
                var today = lastThree[2];

                double previousPercent = ((double)(previous.Nav - beforePrevious.Nav) / (double)beforePrevious.Nav) * 100;
                double todayPercent = ((double)(today.Nav - previous.Nav) / (double)previous.Nav) * 100;

                transformedSchemes.Add(new TransformedScheme
                {
                    SchemeName = today.SchemeName,

                    BeforePreviousDate = beforePrevious.Date,
                    BeforePreviousNav = (double)beforePrevious.Nav,

                    PreviousDate = previous.Date,
                    PreviousNav = (double)previous.Nav,

                    TodayDate = today.Date,
                    TodayNav = (double)today.Nav,

                    PreviousPercent = $"{previousPercent:0.##}%",
                    TodayPercent = $"{todayPercent:0.##}%",

                    IsPreviousIncrease = previous.Nav >= beforePrevious.Nav,
                    IsTodayIncrease = today.Nav >= previous.Nav
                });
            }

            if (!transformedSchemes.Any())
            {
                return new TransformedResult
                {
                    IsSuccess = false,
                    Message = "Not enough NAV data to transform schemes."
                };
            }

            return new TransformedResult
            {
                IsSuccess = true,
                Message = "Success",
                Date1 = transformedSchemes.Max(s => s.TodayDate),
                Date2 = transformedSchemes.Min(s => s.PreviousDate),
                Count = transformedSchemes.Count,
                Schemes = transformedSchemes
            };
        }

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

                    if (currentRecord != null)
                    {
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
                        Nav = currentRecord?.Nav,
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

