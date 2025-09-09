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

        public static (bool IsSuccess, string Message, DateTime StartWorkingDate, DateTime EndWorkingDate, List<DateTime> Dates) GetLastTradingDays(int previousDays = 3)
        {
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

        public static (bool IsSuccess, string Message, DateTime StartWorkingDate, DateTime EndWorkingDate, List<DateTime> Dates) GetWorkingDates(DateTime startDate, DateTime endDate)
        {
            if (endDate > DateTime.Today)
            {
                return (false, "End date cannot be a future date.", default, default, new List<DateTime>());
            }

            // 2. Ensure valid range
            if (startDate > endDate)
            {
                return (false, "Start date must be earlier than or equal to end date.", default, default, new List<DateTime>());
            }

            var allDates = Enumerable.Range(0, (endDate - startDate).Days + 1)
                .Select(offset => startDate.AddDays(offset))
                .Where(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                .ToList();

            string message = "";

            if (allDates.Count > 10)
            {
                message = $"You selected {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}, " +
                          "but only first 10 business days are returned.";
                allDates = allDates.Take(10).ToList();
            }

            return (
                IsSuccess: allDates.Count != 0,
                Message: message,
                StartWorkingDate: allDates.FirstOrDefault(),
                EndWorkingDate: allDates.LastOrDefault(),
                Dates: allDates
            );
        }
    }
}
