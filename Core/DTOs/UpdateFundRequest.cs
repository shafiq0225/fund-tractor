using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public class UpdateFundRequest
    {
        [Required]
        public string FundId { get; set; }
        [Required]
        public bool IsApproved { get; set; }
    }
}
