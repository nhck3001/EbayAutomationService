using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

public class DeepSeekClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public DeepSeekClient(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> SendPrompt(string prompt)
    {
        // Pick the model of DeepSeek
        var body = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                new { role = "system", content = "You are an eBay listing assistant that returns ONLY valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);

        var response = await _http.PostAsync( "https://api.deepseek.com/v1/chat/completions",new StringContent(json, Encoding.UTF8, "application/json")
        );

        // Check for HTTP level failures
        var responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new Exception($"DeepSeek HTTP Error: {response.StatusCode}\n{responseString}");

        // Check for API level failures
        var envelope = JObject.Parse(responseString);
        if (envelope["error"] != null)
            throw new Exception("DeepSeek API Error: " + envelope["error"]);
        // Extract the content and check it
        var content = envelope["choices"]?[0]?["message"]?["content"]?.ToString();
        if (string.IsNullOrWhiteSpace(content))
            throw new Exception("DeepSeek returned empty content.");

        // Clean markdown 
        var cleaned = ExtractJson(content);
        return cleaned;
    }

    /// <summary>
    /// clean input. Input is in the form ''' json...'''. Return json... only
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string ExtractJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        input = input.Trim();

        // Remove ```json ... ``` wrapper
        if (input.StartsWith("```"))
        {
            var firstBrace = input.IndexOf('{');
            var lastBrace = input.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return input.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
        }

        return input;
    }

}
