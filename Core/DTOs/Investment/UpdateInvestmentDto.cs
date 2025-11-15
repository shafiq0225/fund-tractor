using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Investment
{
    public class UpdateInvestmentDto
    {
        [StringLength(50)]
        public string? Status { get; set; }

        public bool? IsPublished { get; set; }

        public bool? IsApproved { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string? Remarks { get; set; }
    }

}
