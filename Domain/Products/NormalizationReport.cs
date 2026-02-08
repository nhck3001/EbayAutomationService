public class NormalizationReport
{
    public string SupplierProductId { get; set; } = "";
    public List<NormalizationFailureReason> Reasons { get; set; } = new();
}
