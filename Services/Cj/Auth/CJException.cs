public class CjRateLimitException : Exception { }
public class CjApiException : Exception
{
    public int Code { get; }

    public CjApiException(int code, string message): base(message)
    {
        Code = code;
    }
}