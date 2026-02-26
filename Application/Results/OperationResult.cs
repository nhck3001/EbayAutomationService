public class OperationResult
{
    public OperationOutcome Outcome { get; init; } // Set it as init to state the fact that the outcome is already defiend in history
    public string? RawMessage { get; init; }

    // Value here is either offerId or listingId
    public string Value { get; init; }

    public bool ShouldRetry => Outcome == OperationOutcome.RetryableFailure;
    public bool IsTerminalFailure => Outcome == OperationOutcome.InvalidData;
    public static OperationResult Success(string value="") =>new() { Outcome = OperationOutcome.Success, Value=value };

    public static OperationResult Invalid(string? message = null) =>new() { Outcome = OperationOutcome.InvalidData, RawMessage = message };

    public static OperationResult Retry(string? message = null) =>new() { Outcome = OperationOutcome.RetryableFailure, RawMessage = message };

    public static OperationResult Exists(string value="", string? message = null) =>new() { Outcome = OperationOutcome.AlreadyExists, Value = value, RawMessage = message };
}
public enum OperationOutcome
{
    Success,
    AlreadyExists,
    InvalidData,
    RetryableFailure
}