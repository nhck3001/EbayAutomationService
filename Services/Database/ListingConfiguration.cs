using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EbayAutomation.Model.Database;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        // Listing object is matched to listings table
        builder.ToTable("listings");
        // Set Id field as primary key
        builder.HasKey(x => x.Id);
        // Set Sku field to column sku
        builder.Property(x => x.Sku).HasColumnName("sku").IsRequired();
        // Set EbayItemId to column ebay_item_id
        builder.Property(x => x.EbayItemId).HasColumnName("ebay_item_id").IsRequired();
        // Set CreatedAt to column created_at.
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        // Create index for sku
        builder.HasIndex(x => x.Sku).IsUnique();
        // Create index for ebay_item_id
        builder.HasIndex(x => x.EbayItemId).IsUnique();
    }
}
