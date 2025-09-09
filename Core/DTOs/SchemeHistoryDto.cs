namespace Core.DTOs
{
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
