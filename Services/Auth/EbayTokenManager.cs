using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;


namespace EbayAutomationService.Services;

/// <summary>
/// This class is used to make sure that access tokens are reused, refreshed only when needed, safe in long-running app.
/// </summary>
public class EbayTokenManager
{
    private readonly EbayAuthService _authService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _accessToken;
    private DateTime _expiresAt = DateTime.MinValue;

    public EbayTokenManager(EbayAuthService authService)
    {
        _authService = authService;

    }

    // Getting access token _authService
    public async Task<string> GetValidTokenAsync()
    {
        // If there is a valid access token simply return it
        if (IsValid())
            return _accessToken!;

        // The lock make sure only 1 thread can request the access token at once
        await _lock.WaitAsync();
        try
        {
            if (IsValid())
                return _accessToken!;

            var token = await _authService.GetAccessTokenAsync();
            _accessToken = token.access_token;
            _expiresAt = DateTime.UtcNow.AddSeconds(token.expires_in);

            return _accessToken!;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ForceRefreshAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var token = await _authService.GetAccessTokenAsync();
            _accessToken = token.access_token;
            _expiresAt = DateTime.UtcNow.AddSeconds(token.expires_in);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// This makes sure that _accessToken is not null and expires in MORE THAN 5 MINUTES
    /// </summary>
    /// <returns></returns>
    private bool IsValid()
    {
        return _accessToken != null &&
               _expiresAt > DateTime.UtcNow.AddMinutes(5);
    }
}
