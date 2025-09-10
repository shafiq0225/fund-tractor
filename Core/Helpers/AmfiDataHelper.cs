using Core.Entities.AMFI;

namespace Core.Helpers
{
    public static class AmfiDataHelper
    {
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

        public static (bool Success, string Message, DateTime StartWorkingDate, DateTime EndWorkingDate, List<DateTime> Dates) GetLastTradingDays(int previousDays = 3)
        {
            try
            {
                if (previousDays < 0)
                {
                    return (false, "previousDays cannot be negative.", default, default, new List<DateTime>());
                }

                var result = new List<DateTime>();
                var current = DateTime.Today;

                // Always include today if it's not weekend
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    result.Add(current.Date);
                }

                // Collect previous working days
                while (result.Count < previousDays + 1) // +1 for today
                {
                    current = current.AddDays(-1);

                    if (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
                        continue;

                    result.Add(current.Date);
                }

                if (!result.Any())
                {
                    return (false, "No trading days found.", default, default, new List<DateTime>());
                }

                // Sort ascending (oldest first)
                result = result.OrderBy(d => d).ToList();

                return (
                    true,
                    "Success",
                    result.First(),   // StartWorkingDate
                    result.Last(),    // EndWorkingDate
                    result
                );
            }
            catch (Exception ex)
            {
                return (false, $"An unexpected error occurred: {ex.Message}", default, default, new List<DateTime>());
            }
        }

        public static (bool Success, string Message, DateTime StartWorkingDate, DateTime EndWorkingDate, List<DateTime> Dates) GetWorkingDates(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (endDate > DateTime.Today)
                {
                    return (false, "End date cannot be a future date.", default, default, new List<DateTime>());
                }

                if (startDate > endDate)
                {
                    return (false, "Start date must be earlier than or equal to end date.", default, default, new List<DateTime>());
                }

                // Range length
                var totalDays = (endDate - startDate).Days + 1;

                if (totalDays > 36500) // safeguard for >100 years
                {
                    return (false, "Date range is too large.", default, default, new List<DateTime>());
                }

                var allDates = Enumerable.Range(0, totalDays)
                    .Select(offset =>
                    {
                        try { return startDate.AddDays(offset); }
                        catch { return DateTime.MinValue; } // fallback if overflow
                    })
                    .Where(d => d != DateTime.MinValue && d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    .ToList();

                string message = "";

                if (allDates.Count > 10)
                {
                    message = $"You selected {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}, but only first 10 business days are returned.";
                    allDates = allDates.Take(10).ToList();
                }

                return (
                    Success: allDates.Count != 0,
                    Message: message,
                    StartWorkingDate: allDates.FirstOrDefault(),
                    EndWorkingDate: allDates.LastOrDefault(),
                    Dates: allDates
                );
            }
            catch (Exception ex)
            {
                return (false, $"An unexpected error occurred: {ex.Message}", default, default, new List<DateTime>());
            }
        }

        public static List<DateTime> GetWorkingDates(DateTime referenceDate, int days)
        {
            var result = new List<DateTime>();
            var date = referenceDate;
            int safetyCounter = 0; // prevent infinite loop

            while (result.Count < days && safetyCounter < 1000)
            {
                if (IsWorkingDay(date))
                {
                    result.Add(date);
                }
                date = date.AddDays(-1);
                safetyCounter++;
            }

            result.Reverse();
            return result;
        }

        public static bool IsWorkingDay(DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday &&
                   date.DayOfWeek != DayOfWeek.Sunday;
        }

        public static decimal? CalculateChange(List<SchemeDetail> records, int days)
        {
            if (records == null || records.Count == 0)
                return null;

            var ordered = records
                .Where(r => r.Nav > 0)
                .OrderByDescending(r => r.Date)
                .Take(days + 1)
                .OrderBy(r => r.Date)
                .ToList();

            if (ordered.Count < 2) return null;

            var start = ordered.First().Nav;
            var end = ordered.Last().Nav;

            if (start <= 0) return null;

            return Math.Round(((end - start) / start) * 100, 2);
        }
    }
}
