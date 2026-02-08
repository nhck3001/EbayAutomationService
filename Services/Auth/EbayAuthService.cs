using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;


namespace EbayAutomationService.Services;

/// <summary>
/// This class is used to exchange _appId, _certId, _refreshToken for accessToken
/// </summary>
public class EbayAuthService
{
    private readonly string _appId;
    private readonly string _certId;
    private readonly string _refreshToken;

    public EbayAuthService(string appId, string certId, string refreshToken)
    {
        _appId = appId;
        _certId = certId;
        _refreshToken = refreshToken;
    }

    // Getting access token from appID, certID/ClientSecretm refreshToken
    public async Task<(string access_token, int expires_in)> GetAccessTokenAsync()
    {
        // Form the authentication string from appID and certID, then Encode it using base64
        var authString = $"{_appId}:{_certId}";
        var authBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

        // HttPClient() object allows sending Http request to api servers
        // Set up the header value
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", authBase64);

        // Set up the content value
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken,
        });

        // Get the response in the form of HttpResponseMessage object
        var response = await client.PostAsync("https://api.ebay.com/identity/v1/oauth2/token", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Token error: {json}");
        }

        // Newtonsoft.Json.Linq.JObject.Parse take a string a convert it into a JObect-dictionary like ot get the value of "access_token"
        var tokenObj = Newtonsoft.Json.Linq.JObject.Parse(json);
        return (tokenObj["access_token"]!.ToString(), tokenObj["expires_in"]!.Value<int>());
    }
}
