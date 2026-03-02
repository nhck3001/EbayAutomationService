using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EbayAutomationService.Infrastructure;
public class InventoryConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        // Maps the Sku C# class to a database table named "skus"
        builder.ToTable("inventory_item");
        // Sets the 'Id' property as the primary key for the skus table
        builder.HasKey(x => x.Id);
        // SKU Code - maps to 'sku' column, cannot be NULL
        builder.Property(x => x.SkuCode).HasColumnName("sku").IsRequired();

        builder.Property(x => x.Ebay_Category_Id).HasColumnName("ebay_category_id");

        // Sell Price - numeric with 10 total digits, 2 decimal places (e.g., 12345678.99), cannot be NULL
        builder.Property(x => x.SellPrice).HasColumnName("sell_price").HasColumnType("numeric(10,2)").IsRequired();
        // Status flag - ENUM with default value of PENDING (not processed yet)
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue(InventoryStatus.Pending);
        // Created At timestamp - defaults to current database time (NOW())
        // Unique index on SkuCode - ensures no duplicate SKU codes can exist
        builder.HasIndex(x => x.SkuCode).IsUnique();
        // Regular index on sku_status - speeds up queries filtering by status
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        // Regular index on CreatedAt - speeds up date-based queries and sorting by creation time
        builder.HasIndex(x => x.CreatedAt);
        // Configures one-to-one relationship between Sku and Inventory entities
        builder.HasOne(x => x.sku).WithOne()                // Sku -> Inventory. Optional 1 to 1                                               
                .HasForeignKey<InventoryItem>(i => i.SkuCode)
                .HasPrincipalKey<Sku>(s => s.SkuCode)
            .OnDelete(DeleteBehavior.Cascade);            // If InventoryItem is deleted, its related sku is also deleted
    }
}