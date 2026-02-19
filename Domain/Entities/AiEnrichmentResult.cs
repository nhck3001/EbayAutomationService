namespace EbayAutomationService.Domain;
public class AiEnrichmentResult
{
    public bool Valid { get; set; }
    public string CategoryName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Type { get; set; } = null!;
    public decimal Sellprice{ get; set; }
    public List<string> Images = []; 
    public Dictionary<string, string> RequiredFields { get; set; } = new();
    public Dictionary<string, string> RecommendedFields { get; set; } = new();
}
