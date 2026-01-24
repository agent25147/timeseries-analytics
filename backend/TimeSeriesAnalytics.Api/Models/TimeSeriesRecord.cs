namespace TimeSeriesAnalytics.Api.Models;

public class TimeSeriesRecord
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public DateTime Timestamp { get; set; }
    public int UserId { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ShippingLane { get; set; } = string.Empty;
    public string ScacCode { get; set; } = string.Empty;  // Standard Carrier Alpha Code
    public string BillToName { get; set; } = string.Empty;
    public Guid InvoiceNumber { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Weight { get; set; }  // in KG
    public DateTime CreatedAt { get; set; }
}
