public class ValidationResult
{
    public bool IsValid => Reasons.Count == 0;

    public List<NormalizationFailureReason> Reasons { get; set; } = new();
}
