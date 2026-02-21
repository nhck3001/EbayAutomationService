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

    Determine whether the product  belongs in this category.
    Return valid = true if the product functionality fit inside the category {categoryName}
    Otherwise return valid = false with rejectReason = REJECT_CATEGORY.

    If Stage 1 fails → return REJECT_CATEGORY immediately.

    ━━━━━━━━━━━━━━━━━━
    STAGE 2 — REQUIRED ITEM SPECIFICS
    ━━━━━━━━━━━━━━━━━━

    Required aspects: {requiredAspectsJson}
    CRITICAL RULES FOR HANDLING CONFLICTING DATA:
    ------------------------------------------------
    1. The input contains multiple data sources: ProductDescription, ProductSpecification, and other fields
    2. When there are CONFLICTS between values:
    - ALWAYS TRUST ProductDescription FIRST (this is the most accurate source)
    - THEN trust ProductSpecification 
    - IGNORE conflicting values from other fields if they contradict these sources
    
    3. For DIMENSIONS specifically:
    - Look in ProductDescription for dimension information (often in text like '12"" x 36"" x 18""')
    - Look in ProductSpecification for structured dimension data
    - If ProductDescription contains detailed dimensions, USE THOSE even if other fields have different values
    - If product dimensions (height/length/width) are NOT available in ProductDescription or ProductSpecification:
        * Look for ""Packing Dimension"" or ""Package Dimension"" information
        * Packing dimensions can be used as a FALLBACK for product dimensions
        * Add a note in the description that these are packed dimensions
    - Convert all dimensions to consistent units (inches), include unit.

    4. For each required aspect:
    • Brand: If no brand information exists, set Brand = ""Unbranded""
    • Type: Infer from product name if needed
    • All other aspects: Use values from ProductDescription or ProductSpecification FIRST.
    • If a required aspect cannot be found in ANY source → REJECT_ASPECT

    5. COMPARISON RULE:
    - Do NOT reject because input fields don't match description
    - INSTEAD: Use the description values as the source of truth
    - The input fields may contain placeholder/default values (like '4 in')
    - Override them with what you find in ProductDescription

If Stage 2 fails → return REJECT_ASPECT immediately.
    ━━━━━━━━━━━━━━━━━━
    TITLE RULES
    ━━━━━━━━━━━━━━━━━━
    Create ebay-friendly titles from input.Name
    • Max 75 characters
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
    • If ""Features"" exists in {recommendedAspectJson}, keep it below 60 characters
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
