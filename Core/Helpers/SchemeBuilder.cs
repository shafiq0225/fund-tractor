using Core.DTOs;
using Core.Entities.AMFI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Helpers
{
    public static class SchemeBuilder
    {
        public static List<SchemeDto> BuildSchemeHistory(List<SchemeDetail> schemes, List<DateTime> allDates, DateTime startDate, DateTime endDate)
        {
            return BuildSchemes(schemes, group =>
            {
                var history = new List<SchemeHistoryDto>();

                foreach (var date in allDates)
                {
                    // Skip boundaries
                    if (date.Date == startDate.Date || date.Date == endDate.Date)
                        continue;

                    var record = FindRecord(group, date);
                    history.Add(CreateHistoryItem(group, record, date));
                }

                return history;
            });
        }

        //public static List<SchemeDto> BuildSchemeHistoryForDaily(List<SchemeDetail> schemes, DateTime endDate, int recordCount = 2)
        //{
        //    return BuildSchemes(schemes, group =>
        //    {
        //        var history = new List<SchemeHistoryDto>();
        //        var currentDate = endDate;

        //        // Keep collecting until we have enough records
        //        while (history.Count < recordCount)
        //        {
        //            var record = FindRecord(group, currentDate);

        //            if (record != null && record.Nav > 0)
        //                history.Add(CreateHistoryItem(group, record, record.Date));

        //            currentDate = currentDate.AddDays(-1);

        //            // Prevent infinite loops if data is missing
        //            if ((endDate - currentDate).TotalDays > 60)
        //                break;
        //        }

        //        return history.OrderBy(h => h.Date).ToList();
        //    });
        //}


        // 🔹 Shared grouping logic
        private static List<SchemeDto> BuildSchemes(List<SchemeDetail> schemes, Func<IGrouping<string, SchemeDetail>, List<SchemeHistoryDto>> buildHistory)
        {
            var schemesList = schemes
                .GroupBy(s => s.SchemeCode) // string key
                .Select(group => new SchemeDto
                {
                    SchemeCode = group.Key, // string
                    FundName = group.First().FundName,
                    SchemeName = group.First().SchemeName,
                    History = buildHistory(group)
                })
                .ToList();

            return schemesList;
        }

        private static SchemeDetail FindRecord(IGrouping<object, SchemeDetail> group, DateTime date)
        {
            return group.FirstOrDefault(r => r.Date.Date == date.Date);
        }


        // 🔹 Shared NAV/Percentage logic
        private static SchemeHistoryDto CreateHistoryItem(IGrouping<object, SchemeDetail> group, SchemeDetail record, DateTime date)
        {
            decimal? nav = null;
            string percentDisplay = null;
            bool isGrowth = false;

            if (record != null)
            {
                nav = Math.Round(record.Nav, 4);

                var prevRecord = group
                    .Where(r => r.Date < date && r.Nav > 0)
                    .OrderByDescending(r => r.Date)
                    .FirstOrDefault();

                if (prevRecord != null)
                {
                    var percentChange = ((record.Nav - prevRecord.Nav) / prevRecord.Nav) * 100;
                    percentChange = Math.Round(percentChange, 2);
                    percentDisplay = $"{percentChange:0.00}";
                    isGrowth = record.Nav > prevRecord.Nav;
                }
            }

            return new SchemeHistoryDto
            {
                Date = date,
                Nav = nav,
                Percentage = percentDisplay ?? "100",
                IsGrowth = isGrowth,
                IsTradingHoliday = record == null
            };
        }

    }

}

