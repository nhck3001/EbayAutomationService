using System.Net.Http.Json;

public class CjAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _refreshToken;

    public CjAuthService(string refreshToken)
    {
        _refreshToken = refreshToken;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://developers.cjdropshipping.com/api2.0/v1/")
        };
    }

    public async Task<CjTokenResponse> RefreshAccessTokenAsync(CancellationToken stoppingToken)
    {
        var payload = new
        {
            refreshToken = _refreshToken
        };

        var response = await _httpClient.PostAsJsonAsync("authentication/refreshAccessToken",payload, stoppingToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return CjTokenResponse.FromJson(json);
    }
}
