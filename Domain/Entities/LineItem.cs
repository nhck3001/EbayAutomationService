using System;

public class LineItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string LineItemId { get; set; }

    public string Sku { get; set; }

    public int Quantity { get; set; }

    public string? CjOrderId { get; set; }

    public string? TrackingNumber { get; set; }

    public string Status { get; set; }

    // Navigation property
    public EbayOrder Order { get; set; }
}
public static class LineItemStatus
{
    public const string Pending = "Pending";
    public const string OrderCreated = "OrderCreated";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Failed = "Failed";

}