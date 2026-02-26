

public class Category
{
    public int Id { get; set; }

    public string EbayCategoryName { get; set; } = null!;
    public int EbayCategoryId { get; set; }
    public string Keyword { get; set; } = null!;

    // Navigation
    public ICollection<DirtySku> DirtySkus { get; set; } = new List<DirtySku>();
}