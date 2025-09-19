using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs
{
    public class ApprovedSchemeDto
    {
        [Required]
        public string FundName { get; set; }
        [Required]
        public string SchemeId { get; set; }
        [Required]
        public string SchemeName { get; set; }
        [Required]
        public bool IsApproved { get; set; }
    }
}
