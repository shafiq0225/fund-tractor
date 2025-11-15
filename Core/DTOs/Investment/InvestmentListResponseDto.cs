using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Investment
{
    public class InvestmentListResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<InvestmentDto> Data { get; set; } = new List<InvestmentDto>();
        public int TotalCount { get; set; }
    }

}
