public class EbayOrderResponse
{
    public string OrderId { get; set; }

    public string OrderPaymentStatus { get; set; }

    public string OrderFulfillmentStatus { get; set; }
    public DateTime PurchaseDate { get; set; }
    public List<EbayLineItem> LineItems { get; set; }
    public EbayBuyer Buyer { get; set; }
        // Convenience accessors

    public string GetBuyerFullName => Buyer?.BuyerRegistrationAddress?.FullName;

    public string GetPrimaryPhone => Buyer?.BuyerRegistrationAddress?.PrimaryPhone?.PhoneNumber;

    public string GetEmail => Buyer?.BuyerRegistrationAddress?.Email;

    public string GetAddressLine1 => Buyer?.BuyerRegistrationAddress?.ContactAddress?.AddressLine1;

    public string GetGetAddressLine2 => Buyer?.BuyerRegistrationAddress?.ContactAddress?.AddressLine2;

    public string GetCity => Buyer?.BuyerRegistrationAddress?.ContactAddress?.City;

    public string GetStateOrProvince => Buyer?.BuyerRegistrationAddress?.ContactAddress?.StateOrProvince;

    public string GetPostalCode => Buyer?.BuyerRegistrationAddress?.ContactAddress?.PostalCode;

    public string GetCountryCode => Buyer?.BuyerRegistrationAddress?.ContactAddress?.CountryCode;

    public string GetPhoneNumber => Buyer?.BuyerRegistrationAddress?.PrimaryPhone?.PhoneNumber;
}


public class EbayLineItem
{
    public string LineItemId { get; set; }

    public string Sku { get; set; }

    public int Quantity { get; set; }
}
public class EbayBuyer
{
    public string Username { get; set; }

    public EbayBuyerRegistrationAddress BuyerRegistrationAddress { get; set; }
}
public class EbayBuyerRegistrationAddress
{
    public string FullName { get; set; }

    public EbayContactAddress ContactAddress { get; set; }

    public EbayPhone PrimaryPhone { get; set; }

    public string Email { get; set; }
}
public class EbayContactAddress
{
    public string AddressLine1 { get; set; }

    public string AddressLine2 { get; set; }

    public string City { get; set; }

    public string StateOrProvince { get; set; }

    public string PostalCode { get; set; }

    public string CountryCode { get; set; }
}
public class EbayPhone
{
    public string PhoneNumber { get; set; }
}