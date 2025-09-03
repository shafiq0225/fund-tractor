using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.AMFI
{
    public class SchemeDetail
    {
        public int Id { get; set; }
        public string FundCode { get; set; }
        public string FundName { get; set; }
        public string SchemeCode { get; set; }
        public string SchemeName { get; set; }
        public bool IsVisible { get; set; }
        public DateTime Date { get; set; }
        public decimal Nav { get; set; }
    }

}
