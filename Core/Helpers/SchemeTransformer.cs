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

            return new TransformedResult
            {
                Date1 = transformedSchemes.Max(s => s.TodayDate),
                Date2 = transformedSchemes.Min(s => s.PreviousDate),
                Count = transformedSchemes.Count,
                Schemes = transformedSchemes
            };
        }
    }

}

