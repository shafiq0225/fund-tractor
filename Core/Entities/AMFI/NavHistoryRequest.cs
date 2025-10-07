using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Entities.AMFI
{
    public class NavHistoryRequest
    {
        public DateTime CurrentDate { get; set; }
    }

    public class NavHistoryResponse
    {
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("schemes")]
        public List<SchemeHistory> Schemes { get; set; } = new List<SchemeHistory>();
    }

    public class SchemeHistory
    {
        [JsonPropertyName("fundName")]
        public string FundName { get; set; } = string.Empty;

        [JsonPropertyName("schemeCode")]
        public string SchemeCode { get; set; } = string.Empty;

        [JsonPropertyName("schemeName")]
        public string SchemeName { get; set; } = string.Empty;

        [JsonPropertyName("history")]
        public List<NavHistory> History { get; set; } = new List<NavHistory>();

        [JsonPropertyName("rank")]
        public int? Rank { get; set; }
    }

    public class NavHistory
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("nav")]
        public decimal Nav { get; set; }

        [JsonPropertyName("percentage")]
        public string Percentage { get; set; } = "100";

        [JsonPropertyName("isTradingHoliday")]
        public bool IsTradingHoliday { get; set; }

        [JsonPropertyName("isGrowth")]
        public bool IsGrowth { get; set; }
    }

    public class NavRecord
    {
        public int Id { get; set; }
        public string FundHouse { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public int IsActive { get; set; }
        public DateTime NavDate { get; set; }
        public decimal NavValue { get; set; }
    }


}
