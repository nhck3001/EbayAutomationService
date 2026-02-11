using Newtonsoft.Json;

public class CrawlState
{
    public int LastEndPage { get; set; } = 0;
    public static async Task<CrawlState> LoadCrawlStateAsync(string path)
    {
        if (!File.Exists(path))
            return new CrawlState();

        var json = await File.ReadAllTextAsync(path);
        return JsonConvert.DeserializeObject<CrawlState>(json) ?? new CrawlState();
    }
    public static async Task SaveCrawlStateAsync(string path, CrawlState state)
    {
        var json = JsonConvert.SerializeObject(state, Formatting.Indented);
        await File.WriteAllTextAsync(path, json);
    }


}
