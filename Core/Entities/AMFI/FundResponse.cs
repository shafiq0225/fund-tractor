using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.AMFI
{
    public class FundResponse
    {
        public string Name { get; set; }
        public int CrisilRank { get; set; }
        public FundReturns Returns { get; set; }
        public List<decimal> MonthlyReturns { get; set; }
    }

    public class FundReturns
    {
        public decimal _1m { get; set; }
        public decimal _6m { get; set; }
        public decimal _1y { get; set; }
        public decimal Yesterday { get; set; }
        public decimal _1week { get; set; }
    }

    public class FundDataResponse : Dictionary<string, FundResponse> { }
}
