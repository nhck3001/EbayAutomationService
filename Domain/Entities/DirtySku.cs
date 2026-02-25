
public class DirtySku
{
    public int Id { get; set; }

    public string Sku { get; set; } = null!;

    public int EbayCategoryId { get; set; }

    public bool Processed { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
}