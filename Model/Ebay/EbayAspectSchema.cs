public class EbayAspectSchema
{
    public List<EbayAspectInfo> RequiredAspects { get; set; } = new();
    public List<EbayAspectInfo> RecommendedAspects { get; set; } = new();
}

public class EbayAspectInfo
{
    public string Name { get; set; } = "";
    public string ValueType { get; set; } = "";
}
