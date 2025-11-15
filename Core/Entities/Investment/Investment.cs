using Core.Entities.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Investment
{
    public class Investment
    {
        public int Id { get; set; }

        [Required]
        public int InvestorId { get; set; }
        public User Investor { get; set; }

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
        public decimal NavRate { get; set; }

        [Required]
        public DateTime DateOfPurchase { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal InvestAmount { get; set; }

        [Required]
        [Range(0.0001, double.MaxValue)]
        public decimal NumberOfUnits { get; set; }

        [Required]
        [StringLength(10)]
        public string ModeOfInvestment { get; set; }

        [StringLength(500)]
        public string? ImagePath { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "in progress";

        public bool IsPublished { get; set; } = false;

        [Required]
        public int CreatedBy { get; set; }
        public User CreatedByUser { get; set; }

        [Required]
        [StringLength(255)]
        public string InvestBy { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}
