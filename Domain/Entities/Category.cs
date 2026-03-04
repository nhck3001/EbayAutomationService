

public class Category
{
    public int Id { get; set; }

    public string EbayCategoryName { get; set; } = null!;
    public int EbayCategoryId { get; set; }
    public List<string> Keyword { get; set; } = null!;
    public bool IsPopulated { get; set; } = false;
    // Navigation
    public ICollection<DirtySku> DirtySkus { get; set; } = new List<DirtySku>();
}