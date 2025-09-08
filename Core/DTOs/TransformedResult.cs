namespace Core.DTOs
{
    public class TransformedResult
    {
        public DateTime Date1 { get; set; }  // Latest NAV date (today)
        public DateTime Date2 { get; set; }  // Previous NAV date
        public int Count { get; set; }       // Number of schemes
        public List<TransformedScheme> Schemes { get; set; } = new List<TransformedScheme>();
    }
}