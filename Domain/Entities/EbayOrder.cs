using System;
using System.Collections.Generic;

public class EbayOrder
{
    public int Id { get; set; }

    public string EbayOrderId { get; set; }

    public DateTime PurchaseDate { get; set; }

    public string BuyerUsername { get; set; }

    public string BuyerFullName { get; set; }

    public string AddressLine1 { get; set; }

    public string AddressLine2 { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string PostalCode { get; set; }

    public string Country { get; set; }

    public string Phone { get; set; }

    public string Email { get; set; }

    public string OrderPaymentStatus { get; set; }

    public string OrderFulfillmentStatus { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public List<LineItem> OrderItems { get; set; } = new();
}