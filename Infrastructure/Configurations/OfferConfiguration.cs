using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EbayAutomationService.Infrastructure;
public class OfferConfiguration : IEntityTypeConfiguration<OfferItem>
{
    public void Configure(EntityTypeBuilder<OfferItem> builder)
    {
        // Listing object is matched to listings table
        builder.ToTable("Offers");
        // Set Id field as primary key
        builder.HasKey(x => x.Id);
        // Set Sku field to column sku
        builder.Property(x =>x.OfferId).HasColumnName("OfferId").IsRequired();
        // Set EbayItemId to column ebay_item_id
        builder.Property(x => x.InventoryId).HasColumnName("InventoryId").IsRequired();
        // Set CreatedAt to column created_at.
        builder.Property(x => x.Quantity).HasColumnName("Quantity").IsRequired();
        builder.Property(x => x.Ebay_Category_Id).HasColumnName("ebay_category_id").IsRequired();

        // Create index for sku
        // Create index for ebay_item_id
        builder.Property(x => x.Status).HasColumnName("Status").HasDefaultValue(InventoryStatus.Pending);
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("NOW()");
        builder.Property(x => x.SellPrice).HasColumnName("SellPrice").HasColumnType("numeric(10,2)").IsRequired();

        // Regular index on CreatedAt - speeds up date-based queries and sorting by creation time
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.OfferId).IsUnique();
        // Configures one-to-one relationship between Inventory and Offer entities
        builder.HasIndex(x => x.InventoryId).IsUnique();
        builder.HasOne(x => x.Inventory).WithOne()                // Sku -> Inventory. Optional 1 to 1                                               
                .HasForeignKey<OfferItem>(o => o.InventoryId)
                .OnDelete(DeleteBehavior.Cascade);         
    }
}
