using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Investment
{
    public class InvestmentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public InvestmentDto? Data { get; set; }
    }
}
