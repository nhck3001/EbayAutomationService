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

        builder.Property(x => x.AvailableInventory).HasColumnName("available_inventory").IsRequired();
        // Status flag - ENUM with default value of PENDING (not processed yet)
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue(InventoryStatus.Pending);

        // Regular index on sku_status - speeds up queries filtering by status
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        // Regular index on CreatedAt - speeds up date-based queries and sorting by creation time
        builder.HasIndex(x => x.CreatedAt);
        // Configures one-to-one relationship between Sku and Inventory entities
        builder.HasIndex(x => x.SkuId).IsUnique();
        builder.HasOne(x => x.sku).WithOne()                // Sku -> Inventory. Optional 1 to 1                                               
                .HasForeignKey<InventoryItem>(i => i.SkuId)
            .OnDelete(DeleteBehavior.Cascade);            // If InventoryItem is deleted, its related sku is also deleted
    }
}