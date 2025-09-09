namespace Core.DTOs
{
    public class TransformedResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? Date1 { get; set; }
        public DateTime? Date2 { get; set; }
        public int Count { get; set; }
        public List<TransformedScheme> Schemes { get; set; } = new List<TransformedScheme>();
    }
}