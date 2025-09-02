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
    }
}
