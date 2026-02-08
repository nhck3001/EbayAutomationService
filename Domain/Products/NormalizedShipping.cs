namespace EbayAutomationService.Domain.Products;

public class NormalizedShipping
{
    public List<string> ShipsFromCountries { get; set; } = new();

    public bool FreeShipping { get; set; }

    public decimal EstimatedWeightGrams { get; set; }
}
