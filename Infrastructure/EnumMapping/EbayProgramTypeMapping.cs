using EbayAutomationService.Domain.Enums;

namespace EbayAutomationService.Infrastructure.Ebay.Mappers;

// Mapping Enum values to string value for EbayProgramType
public static class EbayProgramTypeMapper
{
    public static string ToApiValue(EbayProgramType type) =>
        type switch
        {
            EbayProgramType.OutOfStockControl =>
                "OUT_OF_STOCK_CONTROL",

            EbayProgramType.PartnerMotorsDealer =>
                "PARTNER_MOTORS_DEALER",

            EbayProgramType.SellingPolicyManagement =>
                "SELLING_POLICY_MANAGEMENT",

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
