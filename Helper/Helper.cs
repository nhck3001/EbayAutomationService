using Newtonsoft.Json.Linq;

namespace EbayAutomationService.Helper;
public static class Helper
{
    // Check if a product is likely a shoe organizer
    public static bool IsLikelyShoeOrganizer(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        name = name.ToLowerInvariant();

        if (!name.Contains("shoe"))
            return false;

        string[] storageKeywords =
        {
            "rack",
            "cabinet",
            "organizer",
            "storage",
            "shelf",
            "stand",
            "tower",
            "bench",
            "cupboard",
            "closet"
        };

        return storageKeywords.Any(k => name.Contains(k));
    }

    // Look at requiredAspects.json and return the name of require aspect and its value type
    // - Brand (Type: STRING)
    //Item Height (Type: STRING)
    // aspect is either RequiredAspects or RecommendedAspects
    public static async Task<string> LoadAspectsForPrompt(string filePath, string aspect = "RequiredAspects")
    {
        var formattedAspects = new List<string>();
        if (!File.Exists(filePath))
        {
            throw new Exception("requiredAspects.json not found.");
        }
        // Read file, return a string
        var fullJson = await File.ReadAllTextAsync(filePath);
        // Turn it into manipulatable object
        var obj = JObject.Parse(fullJson);
        var aspects = obj[aspect] as JArray;

        if (aspects == null || aspects.Count == 0)
        {
            throw new Exception($"{aspects} section missing or empty.");
        }
        foreach (var jsonObject in aspects)
        {
            var name = jsonObject["Name"];
            var valueType = jsonObject["ValueType"];
            formattedAspects.Add($"- {name} (Type: {valueType})");
        }
        // Combine all bullet points with line breaks
        return string.Join(Environment.NewLine, formattedAspects);
    }
};
