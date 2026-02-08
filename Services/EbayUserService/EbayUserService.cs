using System.Net.Http.Headers;
using System.Text;

namespace EbayAutomationService.Services;

// Used to retriev the info about the user who authorizes the app
public class EbayUserService
{
    private readonly EbayApiClient _api;


    public EbayUserService(EbayApiClient api)
    {
        _api = api;
    }

    // Get the info of the user
    public async Task<string> GetUserInfo()
    {

        // HttPClient() object allows sending Http request to api servers        
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://api.ebay.com/commerce/identity/v1/user/");
        var response = await _api.SendAsync(request);
        // Get the response in the form of HttpResponseMessage object
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Token error: {json}");
        }

        // Get and return the UserId
        var tokenObj = Newtonsoft.Json.Linq.JObject.Parse(json);
        return tokenObj["userId"]!.ToString();
    }
}
