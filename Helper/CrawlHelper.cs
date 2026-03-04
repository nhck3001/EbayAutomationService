using Newtonsoft.Json.Linq;
using Serilog;

namespace EbayAutomationService.Helper;

public class CrawlHelper
{
    // Get filter based on categoryId   
    public Func<string, bool> GetFilter(int ebayCategoryId)
    {
        switch (ebayCategoryId)
        {
            case 43506: return IsLikelyShoeOrganizer;
            case 22656: return IsLikelyCoatAndHatRack;
            case 36024: return IsLikelyHooksAndHangers;
            case 40620: return IsLikelyUmbrellaStand;
            case 43503: return IsLikelyClosetOrganizer;
            case 43504: return IsLikelyStorageBags;
            case 11673: return IsLikelyClothesHanger;
            case 122772: return IsLikelyDrawerLiners;
            case 122954: return IsLikelyStorageUnit;
            case 159898: return IsLikelyStorageBinsAndBaskets;
            case 159897: return IsLikelyStorageBoxes;
            case 166325: return IsLikelyGarmentRack;
        }

        Log.Error("Couldn't get a filter for category {CategoryId}", ebayCategoryId);
        return _ => false;
    }
    private bool ContainsAny(string name, params string[] keywords)
    {
        return keywords.Any(k => name.Contains(k));
    }
    public bool IsLikelyShoeOrganizer(string? name)
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
    public bool IsLikelyCoatAndHatRack(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }
        name = name.ToLowerInvariant();

        // Must contain "coat" or "hat" l
        if (!name.Contains("coat") && !name.Contains("hat"))
        {
            return false;
        }
        string[] rackKeywords =
        {
            "rack",
            "stand",
            "hook",
            "tree",
            "valet",
            "hanger",
            "organizer",
            "storage"
        };

        return rackKeywords.Any(k => name.Contains(k));
    }

    public bool IsLikelyHooksAndHangers(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!ContainsAny(name, "hook", "hanger"))
            return false;

        return ContainsAny(name,
            "wall",
            "door",
            "coat",
            "adhesive",
            "metal",
            "wood",
            "mount");
    }

    public bool IsLikelyUmbrellaStand(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!name.Contains("umbrella"))
            return false;

        return ContainsAny(name,
            "stand",
            "holder",
            "rack",
            "storage",
            "base");
    }

    public bool IsLikelyClosetOrganizer(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!name.Contains("closet"))
            return false;

        return ContainsAny(name,
            "organizer",
            "storage",
            "shelf",
            "rack",
            "system",
            "wardrobe");
    }

    public bool IsLikelyStorageBags(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!ContainsAny(name, "bag", "storage"))
            return false;

        return ContainsAny(name,
            "clothes",
            "blanket",
            "vacuum",
            "underbed",
            "organizer");

    }
    public bool IsLikelyClothesHanger(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!ContainsAny(name, "hanger"))
            return false;

        return ContainsAny(name,
            "clothes",
            "coat",
            "wood",
            "velvet",
            "plastic",
            "non slip");
    }
    public bool IsLikelyDrawerLiners(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!ContainsAny(name, "liner"))
            return false;

        return ContainsAny(name,
            "drawer",
            "shelf",
            "cabinet",
            "kitchen",
            "non slip");
    }
    public bool IsLikelyStorageUnit(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!ContainsAny(name, "storage"))
            return false;

        return ContainsAny(name,
            "unit",
            "cabinet",
            "shelf",
            "rack",
            "garage");
    }
    public bool IsLikelyStorageBinsAndBaskets(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!ContainsAny(name, "bin", "basket"))
            return false;

        return ContainsAny(name,
            "storage",
            "organizer",
            "plastic",
            "fabric",
            "woven");
    }
    public bool IsLikelyStorageBoxes(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!ContainsAny(name, "box"))
            return false;

        return ContainsAny(name,
            "storage",
            "plastic",
            "stackable",
            "lid",
            "organizer");
    }
    public bool IsLikelyGarmentRack(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.ToLowerInvariant();

        if (!ContainsAny(name, "garment", "clothes"))
            return false;

        return ContainsAny(name,
            "rack",
            "stand",
            "rolling",
            "metal",
            "portable");
    }
}
