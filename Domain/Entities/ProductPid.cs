
public class ProductPid
{
    public int Id { get; set; }

    public string Pid { get; set; } = null!;

    public int CategoryId { get; set; }

    public bool Processed { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
}