using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.AMFI
{
    // Models/PerformanceResponseModels.cs
    public class NavPerformance
    {
        public decimal Nav { get; set; }
        public string Date { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercentage { get; set; }
        public bool IsPositive { get; set; }
    }

    public class SchemePerformanceResponse
    {
        public string Status { get; set; }
        public string SchemeCode { get; set; }
        public string SchemeName { get; set; }
        public string FundHouse { get; set; }
        public decimal CurrentNav { get; set; }
        public string LastUpdated { get; set; }
        public PerformanceData Performance { get; set; }
        public HistoricalData HistoricalData { get; set; }
    }

    public class PerformanceData
    {
        public NavPerformance Yesterday { get; set; }
        public NavPerformance OneWeek { get; set; }
        public NavPerformance OneMonth { get; set; }
        public NavPerformance SixMonths { get; set; }
        public NavPerformance OneYear { get; set; }
    }

    public class HistoricalData
    {
        public List<string> Dates { get; set; }
        public List<decimal> NavValues { get; set; }
    }
}
