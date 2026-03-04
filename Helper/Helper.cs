using Newtonsoft.Json.Linq;

namespace EbayAutomationService.Helper;

public static class Helper
{
    public static string GetAspectJson()
    {
        return "/Users/nhck3001/Documents/GitHub/EbayAutomationService/requiredAspects.json";
    }

    // Look at requiredAspects.json and return both required aspect + recommended aspects and its value type
    // REQUIRED ASPECTS:
    // - Brand (Type: STRING)
    // - Item Height (Type: STRING)
    // - Item Length (Type: STRING)
    //RECOMMENDED ASPECTS:
    //- Material (Type: STRING)
    //- Mounting (Type: STRING)
    public static async Task<string> LoadAspectsForPrompt(string ebayCategoryId, string aspect = "RequiredAspects")

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
        var aspects = chosenCategory[aspect] as JArray;
        // Check if both exist. If not, return a json saying that aspect doesn't exist
        if (aspects == null || aspects.Count == 0)
        {
            if (aspect == "RequiredAspects")
            {
                return "REQUIRED ASPECTS: NONE";
            }
            // recommended aspects
            else
            {
                return "RECOMMENDED ASPECTS: NONE";
            }
        }
        // Start to form the output string. Required Aspects section
        var formattedAspects = new List<string>();

        if (aspect == "RequiredAspects")
        {
            formattedAspects.Add("REQUIRED ASPECTS:");
        }
        else
        {
            formattedAspects.Add("RECOMMENDED ASPECTS:");

        }
        foreach (var jsonObject in aspects)
        {
            formattedAspects.Add($"- {jsonObject["Name"]} (Type: {jsonObject["ValueType"]})");
        }
        return string.Join(Environment.NewLine, formattedAspects);
    }

    public static async Task<String> GetEbayCategoryName(string ebayCategoryId)
    {
        var fullJson = await File.ReadAllTextAsync(GetAspectJson());
        // Get the correct category
        var obj = JObject.Parse(fullJson);
        var categoriesList = obj["Categories"] as JArray;
        var categoryName = categoriesList?.FirstOrDefault(category => category["EbayCategoryId"]?.ToString() == ebayCategoryId)["CategoryName"].ToString();
        return categoryName;
    }
};


   public class EbayCategoryNode
{
    public string CategoryId { get; set; }
    public string CategoryName { get; set; }
    public List<EbayCategoryNode> Children { get; set; } = new();
    public bool IsLeaf => Children == null || Children.Count == 0;
    public static EbayCategoryNode ParseNode(JToken node)
    {
        var category = node["category"];

        var result = new EbayCategoryNode
        {
            CategoryId = category["categoryId"]?.ToString(),
            CategoryName = category["categoryName"]?.ToString()
        };

        var children = node["childCategoryTreeNodes"];

        if (children != null)
        {
            foreach (var child in children)
            {
                result.Children.Add(ParseNode(child));
            }
        }

        return result;
    }

    public static EbayCategoryNode? FindCategory(EbayCategoryNode node, string name)
    {
        if (node.CategoryName.Equals(name, StringComparison.OrdinalIgnoreCase))
            return node;

        foreach (var child in node.Children)
        {
            var found = FindCategory(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    public static void PrintAllChildren(EbayCategoryNode node, int depth = 0)
    {
        string indent = new string(' ', depth * 2);

        Console.WriteLine($"{indent}{node.CategoryName} ({node.CategoryId})");

        foreach (var child in node.Children)
        {
            PrintAllChildren(child, depth + 1);
        }
    }

    public static (EbayCategoryNode? node, EbayCategoryNode? parent)
    FindById(EbayCategoryNode current, string id, EbayCategoryNode? parent = null)
    {
        if (current.CategoryId == id)
            return (current, parent);

        foreach (var child in current.Children)
        {
            var result = FindById(child, id, current);
            if (result.node != null)
                return result;
        }

        return (null, null);
    }
    public static List<EbayCategoryNode> GetSiblingLeafCategories(JToken treeJson, string targetCategoryId)
    {
        var rootNodeJson = treeJson["rootCategoryNode"];
        var rootNode = EbayCategoryNode.ParseNode(rootNodeJson);
        var (node, parent) = FindById(rootNode, targetCategoryId);
        if (node == null || parent == null)
        {
            return new List<EbayCategoryNode>();
   
        }
        var siblings = parent.Children.Where(c => c.IsLeaf).ToList();
        foreach (var cat in siblings)
        {
            Console.WriteLine($"{cat.CategoryName} ({cat.CategoryId})");
        }
        return siblings;
    }
        }