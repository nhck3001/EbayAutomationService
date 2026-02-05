// Model for ResponseFrom Cj that includes a product list
public class CjProductListResponse
{
    public int Code { get; set; }
    public bool Result { get; set; }
    public string Message { get; set; }
    // Inside here is additional info + PRODUCT DATA
    public CjProductListData Data { get; set; }

    public string RequestId { get; set; }
    public bool Success { get; set; }
}
