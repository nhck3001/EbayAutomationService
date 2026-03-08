using System;

public class LineItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string LineItemId { get; set; }

    public string Sku { get; set; }

    public int Quantity { get; set; }

    public string CjOrderId { get; set; }

    public string TrackingNumber { get; set; }

    public string Status { get; set; }

    // Navigation property
    public EbayOrder Order { get; set; }
}