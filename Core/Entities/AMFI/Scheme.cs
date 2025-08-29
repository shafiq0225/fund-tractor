using System;

namespace Core.Entities.AMFI;

public class Scheme
{
    public string SchemeId { get; set; } 
    public string FundId { get; set; }
    public Fund Fund { get; set; }
    public string SchemeName { get; set; }
    public bool IsManagerApproved { get; set; } = false;
    public bool IsVisible { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    public string ApprovedBy { get; set; }
}
