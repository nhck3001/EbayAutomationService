// Model for the list of product itself
public class CjProductListData
{
    // Tell you what page you are on
    public int PageNum { get; set; }
    // How many items per page
    public int PageSize { get; set; }

    // how many total items exist
    public long Total { get; set; }

    // a list of products
    public List<CjProductDetail> List { get; set; }
}
