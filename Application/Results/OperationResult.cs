public class OperationResult
{
    public OperationOutcome Outcome { get; init; }
    public string? RawMessage { get; init; }
}
public enum OperationOutcome
{
    Success,
    AlreadyExists,
    InvalidData,
    RetryableFailure
}