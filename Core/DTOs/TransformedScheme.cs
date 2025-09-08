using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public class TransformedScheme
    {
        public string SchemeName { get; set; }

        public DateTime BeforePreviousDate { get; set; }
        public double BeforePreviousNav { get; set; }

        public DateTime PreviousDate { get; set; }
        public double PreviousNav { get; set; }

        public DateTime TodayDate { get; set; }
        public double TodayNav { get; set; }

        public string PreviousPercent { get; set; }
        public string TodayPercent { get; set; }

        public bool IsPreviousIncrease { get; set; }
        public bool IsTodayIncrease { get; set; }
    }
}
