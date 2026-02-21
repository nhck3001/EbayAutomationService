public class OperationResult
{
    public OperationOutcome Outcome { get; init; }
    public string? RawMessage { get; init; }
    public bool ShouldRetry => Outcome == OperationOutcome.RetryableFailure;
    public bool IsTerminalFailure => Outcome == OperationOutcome.InvalidData;
}
public enum OperationOutcome
{
    Success,
    AlreadyExists,
    InvalidData,
    RetryableFailure
}