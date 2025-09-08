using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Helpers
{
    public static class AmfiDataHelper
    {
        public static bool IsHeaderOrSection(string line)
        {
            return line.StartsWith("Scheme Code") || line.Contains("Open Ended") || string.IsNullOrWhiteSpace(line);
        }

        public static bool IsFundLine(string line)
        {
            return !line.Contains(";");
        }

        public static string GenerateFundId(string fundName)
        {
            if (string.IsNullOrWhiteSpace(fundName))
                return string.Empty;

            const string suffix = "Mutual Fund";
            string result;

            if (fundName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                result = fundName.Substring(0, fundName.Length - suffix.Length).Trim().Replace(" ", "");
                result += "_MF";
            }
            else
            {
                result = fundName.Replace(" ", "");
            }

            return result;
        }

        public static (DateTime Date1, DateTime Date2) GetLastTwoWorkingDays(DateTime today)
        {
            // Start from yesterday
            var date1 = today.AddDays(-2);
            var date2 = today.AddDays(-1);

            // If today is Monday → set to Friday & Thursday
            if (today.DayOfWeek == DayOfWeek.Monday)
            {
                date1 = today.AddDays(-4); // Friday
                date2 = today.AddDays(-3); // Thursday
            }
            // If today is Sunday → Friday & Thursday
            else if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                date1 = today.AddDays(-3); // Friday
                date2 = today.AddDays(-2); // Thursday
            }
            // If today is Saturday → Friday & Thursday
            else if (today.DayOfWeek == DayOfWeek.Saturday)
            {
                date1 = today.AddDays(-2); // Friday
                date2 = today.AddDays(-1); // Thursday
            }

            return (date1.Date, date2.Date);
        }

        public static (DateTime Date1, DateTime Date3) GetLastThreeWorkingDays(DateTime today)
        {
            var marketDays = new List<DateTime>();
            var current = today.AddDays(-1); // start from yesterday

            while (marketDays.Count < 3)
            {
                if (current.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday or DayOfWeek.Monday))
                {
                    marketDays.Add(current.Date);
                }

                current = current.AddDays(-1);
            }

            // Return them sorted (most recent first)
            return (marketDays[2], marketDays[0]);
        }


        public static (DateTime Date1, DateTime Date2) GetDateRangeOrLastTwoWorkingDays(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                var s = startDate.Value.Date;
                var e = endDate.Value.Date;

                // Validation: Start must be <= End
                if (s > e)
                    throw new ArgumentException("Start date must be earlier than end date.");

                // Validation: Max 10 days
                if ((e - s).TotalDays > 10)
                    throw new ArgumentException("One can download historical NAV for a maximum period of 10 days at a time.");

                return (s, e);
            }

            // ✅ Fallback: Last 2 working days (weekend skip logic)
            var today = DateTime.Today;
            var date1 = today.AddDays(-1);
            var date2 = today.AddDays(-2);

            if (today.DayOfWeek == DayOfWeek.Monday)
            {
                date1 = today.AddDays(-3); // Friday
                date2 = today.AddDays(-4); // Thursday
            }
            else if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                date1 = today.AddDays(-2); // Friday
                date2 = today.AddDays(-3); // Thursday
            }
            else if (today.DayOfWeek == DayOfWeek.Saturday)
            {
                date1 = today.AddDays(-1); // Friday
                date2 = today.AddDays(-2); // Thursday
            }

            return (date1.Date, date2.Date);
        }

    }
}
