using System;
using System.Collections.Generic;

namespace Core.DTOs
{
    /// <summary>
    /// Root response wrapper
    /// </summary>
    public class SchemeResponseDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Message { get; set; }
        public List<SchemeDto> Schemes { get; set; } = new();
    }

    /// <summary>
    /// A single scheme with NAV history
    /// </summary>
    public class SchemeDto
    {
        public string FundName { get; set; }
        public string SchemeCode { get; set; }
        public string SchemeName { get; set; }
        public List<SchemeHistoryDto> History { get; set; } = new();
    }

    /// <summary>
    /// Daily NAV details for a scheme
    /// </summary>
    public class SchemeHistoryDto
    {
        public DateTime Date { get; set; }
        public decimal? Nav { get; set; }
        public string Percentage { get; set; }
        public bool IsTradingHoliday { get; set; }
        public bool IsGrowth { get; set; }
    }
}
