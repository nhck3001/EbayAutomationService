

public class Category
{
    public int Id { get; set; }

    public string EbayCategoryName { get; set; } = null!;
    public string EbayCategoryId { get; set; } = null!;
    public string Keyword { get; set; } = null!;

    // Navigation
    public ICollection<ProductPid> ProductPids { get; set; } = new List<ProductPid>();
}