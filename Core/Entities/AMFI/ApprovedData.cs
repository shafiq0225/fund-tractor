using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.AMFI
{
    public class ApprovedData
    {
        public int Id { get; set; }
        public string FundCode { get; set; }
        public string SchemeCode { get; set; }
        public string SchemeName { get; set; }
        public bool IsApproved { get; set; }
        public string ApprovedName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedDate { get; set; }
    }

}
