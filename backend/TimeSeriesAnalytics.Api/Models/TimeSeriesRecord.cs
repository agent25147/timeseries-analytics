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


public class TimeSeriesRecordDto
{
    // UUIDs mapped to Guid
    public Guid id { get; set; }
    public Guid transaction_id { get; set; }
    public Guid invoice_number { get; set; }

    // Other fields
    public DateTime timestamp { get; set; }
    public int user_id { get; set; }
    public string region { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public string product_name { get; set; } = string.Empty;

    // Decimal fields
    public decimal amount { get; set; }
    public int quantity { get; set; }

    // Strings
    public string status { get; set; } = string.Empty;
    public string payment_method { get; set; } = string.Empty;
    public string address { get; set; } = string.Empty;
    public string shipping_lane { get; set; } = string.Empty;
    public string scac_code { get; set; } = string.Empty;
    public string bill_to_name { get; set; } = string.Empty;
    public string currency_code { get; set; } = string.Empty;

    // Decimal field
    public decimal weight { get; set; }

    // DateTime
    public DateTime created_at { get; set; }
}
