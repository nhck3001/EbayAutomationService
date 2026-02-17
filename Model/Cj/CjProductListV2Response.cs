using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EbayAutomationService.Models
{
    // Root response class
    public class CjProductListV2Response
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        
        [JsonProperty("result")]
        public bool Result { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("data")]
        public CjProductData Data { get; set; }
        
        [JsonProperty("requestId")]
        public string RequestId { get; set; }
        
        [JsonProperty("success")]
        public bool Success { get; set; }
    }

    // Data container
    public class CjProductData
    {
        [JsonProperty("content")]
        public List<CjProductContent> Content { get; set; }
    }

    // Content wrapper
    public class CjProductContent
    {
        [JsonProperty("productList")]
        public List<CjProduct> ProductList { get; set; }
        
        [JsonProperty("relatedCategoryList")]
        public List<CjRelatedCategory> RelatedCategoryList { get; set; }
        
        [JsonProperty("keyWord")]
        public string KeyWord { get; set; }
        
        [JsonProperty("keyWordOld")]
        public string KeyWordOld { get; set; }
    }

    // Main product class
    public class CjProduct
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("nameEn")]
        public string NameEn { get; set; }
        
        [JsonProperty("sku")]
        public string Sku { get; set; }
        
        [JsonProperty("spu")]
        public string Spu { get; set; }
        
        [JsonProperty("bigImage")]
        public string BigImage { get; set; }
        
        [JsonProperty("sellPrice")]
        public string SellPrice { get; set; }
        
        [JsonProperty("nowPrice")]
        public string NowPrice { get; set; }
        
        [JsonProperty("listedNum")]
        public int ListedNum { get; set; }
        
        [JsonProperty("categoryId")]
        public string CategoryId { get; set; }
        
        [JsonProperty("threeCategoryName")]
        public string ThreeCategoryName { get; set; }
        
        [JsonProperty("twoCategoryId")]
        public string TwoCategoryId { get; set; }
        
        [JsonProperty("twoCategoryName")]
        public string TwoCategoryName { get; set; }
        
        [JsonProperty("oneCategoryId")]
        public string OneCategoryId { get; set; }
        
        [JsonProperty("oneCategoryName")]
        public string OneCategoryName { get; set; }
        
        [JsonProperty("addMarkStatus")]
        public int AddMarkStatus { get; set; }
        
        [JsonProperty("isVideo")]
        public int IsVideo { get; set; }
        
        [JsonProperty("videoList")]
        public List<object> VideoList { get; set; } // Empty array in example, could be List<string> if videos exist
        
        [JsonProperty("productType")]
        public string ProductType { get; set; }
        
        [JsonProperty("supplierName")]
        public string SupplierName { get; set; }
        
        [JsonProperty("createAt")]
        public long CreateAt { get; set; } // Unix timestamp in milliseconds
        
        [JsonProperty("warehouseInventoryNum")]
        public int WarehouseInventoryNum { get; set; }
        
        [JsonProperty("totalVerifiedInventory")]
        public int TotalVerifiedInventory { get; set; }
        
        [JsonProperty("totalUnVerifiedInventory")]
        public int TotalUnVerifiedInventory { get; set; }
        
        [JsonProperty("verifiedWarehouse")]
        public int VerifiedWarehouse { get; set; }
        
        [JsonProperty("customization")]
        public int Customization { get; set; }
        
        [JsonProperty("hasCECertification")]
        public int HasCECertification { get; set; }
        
        [JsonProperty("isCollect")]
        public int IsCollect { get; set; }
        
        [JsonProperty("myProduct")]
        public bool MyProduct { get; set; }
        
        [JsonProperty("currency")]
        public string Currency { get; set; }
        
        [JsonProperty("discountPrice")]
        public string DiscountPrice { get; set; }
        
        [JsonProperty("discountPriceRate")]
        public string DiscountPriceRate { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("deliveryCycle")]
        public string DeliveryCycle { get; set; }
        
        [JsonProperty("saleStatus")]
        public string SaleStatus { get; set; }
        
        [JsonProperty("authorityStatus")]
        public string AuthorityStatus { get; set; }
        
        [JsonProperty("isPersonalized")]
        public int IsPersonalized { get; set; }
    }

    // Related category class
    public class CjRelatedCategory
    {
        [JsonProperty("categoryId")]
        public string CategoryId { get; set; }
        
        [JsonProperty("categoryName")]
        public string CategoryName { get; set; }
    }
}