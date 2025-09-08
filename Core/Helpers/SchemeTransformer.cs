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
            var transformedSchemes = schemes
                .GroupBy(s => s.SchemeName)
                .Select(g =>
                {
                    var ordered = g.OrderBy(s => s.Date).ToList();
                    if (ordered.Count < 3) return null; // need at least 3 NAVs

                    var before = ordered[ordered.Count - 3];
                    var previous = ordered[ordered.Count - 2];
                    var today = ordered[ordered.Count - 1];

                    double previousPercent = ((double)(previous.Nav - before.Nav) / (double)before.Nav) * 100;
                    double todayPercent = ((double)(today.Nav - previous.Nav) / (double)previous.Nav) * 100;

                    return new TransformedScheme
                    {
                        SchemeName = today.SchemeName,
                        PreviousDate = previous.Date,
                        PreviousNav = (double)previous.Nav,
                        TodayDate = today.Date,
                        TodayNav = (double)today.Nav,
                        PreviousPercent = $"{previousPercent:0.##}%",
                        TodayPercent = $"{todayPercent:0.##}%",
                        IsPreviousIncrease = previous.Nav >= before.Nav,
                        IsTodayIncrease = today.Nav >= previous.Nav
                    };
                })
                .Where(x => x != null)
                .ToList();

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
