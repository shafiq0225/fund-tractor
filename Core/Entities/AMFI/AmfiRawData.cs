using System;

namespace Core.Entities.AMFI;

public class AmfiRawData
{
    public int Id { get; set; }
    public string FundId { get; set; }

    public string FundName { get; set; }
    public string SchemeName { get; set; }
    public string SchemeCode { get; set; }
    public decimal NetAssetValue { get; set; }
    public DateTime Date { get; set; }
    public bool IsVisible { get; set; }
}
