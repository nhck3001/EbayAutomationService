using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class DeepSeekService
{
    private readonly DeepSeekClient _ai;

    public DeepSeekService(DeepSeekClient ai)
    {
        _ai = ai;
    }

public static string BuildPrompt(string input, string requiredAspectsJson, string recommendedAspectJson, string categoryName)
    {
        return $@"
    You are a strict eBay listing validator.

    Your job:
    Determine if this supplier variant can become a REAL eBay listing.

    ━━━━━━━━━━━━━━━━━━
    STAGE 1 — CATEGORY CHECK ({categoryName} ONLY)
    ━━━━━━━━━━━━━━━━━━

    Target eBay Category:
    {categoryName}

    Determine whether the product clearly and specifically belongs in this category.
    Return valid = true ONLY if:
    - The product is primarily designed for {categoryName}
    - The product's core function matches {categoryName}
    Otherwise return valid = false with rejectReason = REJECT_CATEGORY.

    If Stage 1 fails → return REJECT_CATEGORY immediately.

    ━━━━━━━━━━━━━━━━━━
    STAGE 2 — REQUIRED ITEM SPECIFICS
    ━━━━━━━━━━━━━━━━━━

    Required aspects: {requiredAspectsJson}
    1. For brand
    • If Brand is a required aspect and no brand information exists in input
    • Set Brand = ""Unbranded""
    • Do NOT reject for missing Brand
    
    2. For Type
    • If ""Type"" is a required aspect, infer it from the product name
    • Do not leave Type blank if it can be inferred

    3. For others. 
    • Use ONLY values present in {input}
    • Do NOT invent missing values
    • If any required aspect cannot be infered → REJECT_ASPECT
    
    If Stage 2 fails → return REJECT_CATEGORY immediately.

    ━━━━━━━━━━━━━━━━━━
    TITLE RULES
    ━━━━━━━━━━━━━━━━━━
    Create ebay-friendly titles from input.dName
    • Max 80 characters
    • No emojis
    • No sales phrases
    • Include material or color if available

    ━━━━━━━━━━━━━━━━━━
    DESCRIPTION RULES
    ━━━━━━━━━━━━━━━━━━

    If input.Description has meaningful value:
    Create professional eBay HTML description.

    REMOVE COMPLETELY supplier logistics, packaging data, and internal identifiers.
    Keep ONLY customer-relevant product information:

    Formatting:
    • Use short paragraphs
    • Use bullet points
    • Neutral professional tone
    • No ALL CAPS
    • No marketing exaggeration

    ━━━━━━━━━━━━━━━━━━
    RECOMMENDED FIELDS RULES
    ━━━━━━━━━━━━━━━━━━
    Recommended aspects:
    {recommendedAspectJson}
    • Only fill aspects listed in {recommendedAspectJson}
    • Do NOT duplicate required aspects.
    • Do NOT return fields that are already in {requiredAspectsJson}.
    • If uncertain, omit the field. Do NOT guess.

    ━━━━━━━━━━━━━━━━━━
    IMAGE RULES
    ━━━━━━━━━━━━━━━━━━
    Examine input.Images
    Return a cleaned array of image URLs:
    • Remove nulls
    • Remove duplicates
    • Remove invalid URLs
    • Maximum 6 images
  
    ━━━━━━━━━━━━━━━━━━
    OUTPUT (STRICT JSON ONLY)
    ━━━━━━━━━━━━━━━━━━
    Return ONLY valid JSON.
    Do NOT include markdown.
    Do NOT include commentary.
    
    If rejected:

    {{
    ""valid"": false,
    ""rejectReason"": ""REJECT_CATEGORY"" | ""REJECT_ASPECT"",
    ""message"": ""reason""
    }}

    If valid:

    {{
    ""valid"": true,
    ""categoryName"": "",
    ""title"": "",
    ""description"": "",
    ""images"": [],
    ""type"", ""
    ""sellPrice"","",
    ""requiredFields"": "",
    ""recommendedFields"": ""
    }}

    ━━━━━━━━━━━━━━━━━━
    INPUT
    ━━━━━━━━━━━━━━━━━━

    {input}
    ";
    }

        


}
