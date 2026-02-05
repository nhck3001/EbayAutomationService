public class CjTokenManager
{
    private readonly CjAuthService _authService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _accessToken;
    private DateTime _expiresAt = DateTime.MinValue;

    public CjTokenManager(CjAuthService authService)
    {
        _authService = authService;
    }

    // Return access token
    public async Task<string> GetValidTokenAsync()
    {
        if (IsValid())
            return _accessToken!;

        await _lock.WaitAsync();
        try
        {
            if (IsValid())
                return _accessToken!;
            // If token is expired in less than 12 hours, get a new one and return it
            var token = await _authService.RefreshAccessTokenAsync();

            _accessToken = token.AccessToken;
            _expiresAt = token.AccessTokenExpiryUtc;

            return _accessToken!;
        }
        finally
        {
            _lock.Release();
        }
    }

    // Token is valid if expire in more than 12 hours
    private bool IsValid()
    {
        return _accessToken != null &&
               _expiresAt > DateTime.UtcNow.AddHours(12); // CJ tokens live long
    }
}
