namespace Core.DTOs
{
    /// <summary>
    /// A single scheme with NAV history
    /// </summary>
    public class SchemeDto
    {
        public string FundName { get; set; }
        public string SchemeCode { get; set; }
        public string SchemeName { get; set; }
        public List<SchemeHistoryDto> History { get; set; } = new();

        // Nullable rank: only top 4 schemes get a value
        public int? Rank { get; set; } = null;

    }
}
