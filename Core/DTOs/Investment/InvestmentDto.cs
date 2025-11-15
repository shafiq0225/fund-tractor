using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Investment
{
    public class InvestmentDto
    {
        public int Id { get; set; }
        public int InvestorId { get; set; }
        public string InvestorName { get; set; }
        public string SchemeCode { get; set; }
        public string SchemeName { get; set; }
        public string FundName { get; set; }
        public decimal NavRate { get; set; }
        public DateTime DateOfPurchase { get; set; }
        public decimal InvestAmount { get; set; }
        public decimal NumberOfUnits { get; set; }
        public string ModeOfInvestment { get; set; }
        public string ImagePath { get; set; }
        public string Status { get; set; }
        public bool IsPublished { get; set; }
        public string InvestBy { get; set; }
        public string CreatedByUserName { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Remarks { get; set; }
    }

}
