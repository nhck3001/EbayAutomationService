using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EbayAutomationService.Infrastructure;
public class SkuConfiguration : IEntityTypeConfiguration<Sku>
{
    public void Configure(EntityTypeBuilder<Sku> builder)
    {
        // Maps the Sku C# class to a database table named "skus"
        builder.ToTable("skus");
        // Sets the 'Id' property as the primary key for the skus table
        builder.HasKey(x => x.Id);
        // SKU Code - maps to 'sku' column, cannot be NULL
        builder.Property(x => x.SkuCode).HasColumnName("sku").IsRequired();
        // PID (Product ID) - maps to 'pid' column, cannot be NULL
        builder.Property(x => x.Pid).HasColumnName("pid").IsRequired();
        // Product Title - maps to 'title' column, cannot be NULL
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        // Product Description - maps to 'description' column, cannot be NULL
        builder.Property(x => x.Description).HasColumnName("description").IsRequired();
        // Image URLs - stored as PostgreSQL text array (text[]), cannot be NULL
        builder.Property(x => x.ImageUrls).HasColumnName("image_urls").HasColumnType("text[]").IsRequired();
        // Item Specifics - stored as JSONB for flexible key-value pairs, cannot be NULL
        builder.Property(x => x.ItemSpecifics).HasColumnName("item_specifics").HasColumnType("jsonb").IsRequired();
        // Sell Price - numeric with 10 total digits, 2 decimal places (e.g., 12345678.99), cannot be NULL
        builder.Property(x => x.SellPrice).HasColumnName("sell_price").HasColumnType("numeric(10,2)").IsRequired();
        // Processed flag - boolean with default value of FALSE (not processed yet)
        builder.Property(x => x.SkuStatus).HasColumnName("processed").HasDefaultValue(false);
        // Created At timestamp - defaults to current database time (NOW())
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        // Unique index on SkuCode - ensures no duplicate SKU codes can exist
        builder.HasIndex(x => x.SkuCode).IsUnique();
        // Regular index on Processed - speeds up queries filtering by processed/unprocessed status
        builder.HasIndex(x => x.SkuStatus);
        // Regular index on CreatedAt - speeds up date-based queries and sorting by creation time
        builder.HasIndex(x => x.CreatedAt);
        // Configures one-to-one relationship between Sku and Listing entities
        builder.HasOne(x => x.Listing)                    // Sku has one Listing
            .WithOne(x => x.SkuEntity)                    // Listing has one SkuEntity
            .HasForeignKey<Listing>(x => x.Sku)           // Foreign key is Listing.Sku property
            .HasPrincipalKey<Sku>(x => x.SkuCode)         // References Sku.SkuCode (not the Id)
            .OnDelete(DeleteBehavior.Cascade);            // If Sku is deleted, its related Listing is also deleted
    }
}