public static class CategoryHelper
{
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
};
