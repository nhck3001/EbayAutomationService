using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;


namespace EbayAutomationService.Services;

/// <summary>
/// Every API request flow through this class. 
/// It is responsible for setting up the request, send it, and retrieve it.
/// </summary>
public class EbayApiClient
{
    private readonly HttpClient _client;
    private readonly EbayTokenManager _tokenManager;

    public EbayApiClient(HttpClient client, EbayTokenManager tokenManager)
    {
        _client = client;
        _tokenManager = tokenManager;
    }

    /// <summary>
    /// This set up the header, return the response, refresh the access Token and send the request again if it expires
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool settingHeaderContentLanguage = false)
    {
        var token = await _tokenManager.GetValidTokenAsync();
        // Setting up headers
        request.Headers.Authorization =new AuthenticationHeaderValue("Bearer", token);
        if (settingHeaderContentLanguage)
        {
            request.Content!.Headers.Add("Content-Language", "en-US");

        }
        var response = await _client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _tokenManager.ForceRefreshAsync();

            token = await _tokenManager.GetValidTokenAsync();
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            response = await _client.SendAsync(request);
        }

        return response;
    }
}
