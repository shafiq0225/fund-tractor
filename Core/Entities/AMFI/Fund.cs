using System;

namespace Core.Entities.AMFI;

public class Fund
{
    public string FundId { get; set; }
    public string FundName { get; set; }
    public bool IsManagerApproved { get; set; } = false;
    public bool IsVisible { get; set; } = false;
    public ICollection<Scheme> Schemes { get; set; } = new List<Scheme>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    public string ApprovedBy { get; set; }
}
