using Newtonsoft.Json.Linq;

public class CjTokenResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime AccessTokenExpiryUtc { get; set; }

    public static CjTokenResponse FromJson(string json)
    {
        var root = JObject.Parse(json)["data"]!;

        return new CjTokenResponse
        {
            AccessToken = root["accessToken"]!.ToString(),
            AccessTokenExpiryUtc = DateTime.Parse(
                root["accessTokenExpiryDate"]!.ToString()
            ).ToUniversalTime()
        };
    }
}
