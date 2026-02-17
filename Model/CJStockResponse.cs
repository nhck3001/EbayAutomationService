// Class to Model response from 
// https://developers.cjdropshipping.com/api2.0/v1/product/stock/queryBySku?sku=CJDS2012593'
public class CjStockBySkuResponse
{
    public int Code { get; set; }
    public bool Result { get; set; }
    public string Message { get; set; }
    public List<CjStockWarehouse> Data { get; set; }
    public string RequestId { get; set; }
    public bool Success { get; set; }


}

public class CjStockWarehouse
{
    public string AreaEn { get; set; }
    public int AreaId { get; set; }
    public string CountryCode { get; set; }
    public int? TotalInventoryNum { get; set; }
    public int? CjInventoryNum { get; set; }
    public int? FactoryInventoryNum { get; set; }
    public string CountryNameEn { get; set; }
    public List<CjStockItem> Stock { get; set; }
}

public class CjStockItem
{
    public string StockId { get; set; }
    public int? Inventory { get; set; }
    public int? FactoryInventory { get; set; }
}
