using Newtonsoft.Json.Linq;

namespace EbayAutomationService.Helper;
public static class Helper
{
    public static string GetAspectJson()
    {
        return "/Users/nhck3001/Documents/GitHub/EbayAutomationService/requiredAspects.json";
    }
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

    // Look at requiredAspects.json and return both required aspect + recommended aspects and its value type
    // REQUIRED ASPECTS:
    // - Brand (Type: STRING)
    // - Item Height (Type: STRING)
    // - Item Length (Type: STRING)
    //RECOMMENDED ASPECTS:
    //- Material (Type: STRING)
    //- Mounting (Type: STRING)
    public static async Task<string> LoadAspectsForPrompt(string ebayCategoryId)
    {
        //Check if file exists
        if (!File.Exists(GetAspectJson()))
        {
            throw new Exception("requiredAspects.json not found.");
        }
        // Read file, return a string
        var fullJson = await File.ReadAllTextAsync(GetAspectJson());
        // Get the correct category
        var obj = JObject.Parse(fullJson);
        var categoriesList = obj["Categories"] as JArray;
        var chosenCategory = categoriesList?.FirstOrDefault(category => category["EbayCategoryId"]?.ToString() == ebayCategoryId);
        // Check if aspects for {ebayCategoryId} exists
        if (chosenCategory == null)
        {
            throw new Exception("Category not found in aspects.json");
        }
        // Get requiredAspects and recommendedAspects
        var requiredAspects = chosenCategory["RequiredAspects"] as JArray;
        var recommendedAspects = chosenCategory["RecommendedAspects"] as JArray;
        // Check if both exist
        if (requiredAspects == null || requiredAspects.Count == 0)
        {
            throw new Exception($"Required aspects section section missing or empty.");
        }
        if (recommendedAspects == null || recommendedAspects.Count == 0)
        {
            throw new Exception($"Required aspects section section missing or empty.");
        }
        // Start to form the output string. Required Aspects section
        var formattedAspects = new List<string>();
        formattedAspects.Add("REQUIRED ASPECTS:");
        foreach (var jsonObject in requiredAspects)
        {
            formattedAspects.Add($"- {jsonObject["Name"]} (Type: {jsonObject["ValueType"]})");
        }
        //form the output string. Recommended Aspects section
        formattedAspects.Add("");
        formattedAspects.Add("RECOMMENDED ASPECTS:");

        foreach (var jsonObject in recommendedAspects)
        {
            formattedAspects.Add($"- {jsonObject["Name"]} (Type: {jsonObject["ValueType"]})");
        }
        // Combine all bullet points with line breaks
        return string.Join(Environment.NewLine, formattedAspects);
    }
};
