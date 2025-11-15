using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Investment
{
    public class CreateInvestmentDto
    {
        [Required]
        public int InvestorId { get; set; }

        [Required]
        [StringLength(20)]
        public string SchemeCode { get; set; }

        [Required]
        [StringLength(255)]
        public string SchemeName { get; set; }

        [Required]
        [StringLength(255)]
        public string FundName { get; set; }

        [Required]
        public Decimal NavRate { get; set; }

        [Required]
        public DateTime DateOfPurchase { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal InvestAmount { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue)]
        public decimal NumberOfUnits { get; set; }

        [Required]
        [RegularExpression("^(online|offline)$", ErrorMessage = "Mode must be either 'online' or 'offline'")]
        public string ModeOfInvestment { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string? Remarks { get; set; }
    }

}
