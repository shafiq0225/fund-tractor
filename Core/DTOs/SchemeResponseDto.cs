using System;
using System.Collections.Generic;

namespace Core.DTOs
{
    /// <summary>
    /// Root response wrapper
    /// </summary>
    public class SchemeResponseDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Message { get; set; }
        public List<SchemeDto> Schemes { get; set; } = new();
    }
}
