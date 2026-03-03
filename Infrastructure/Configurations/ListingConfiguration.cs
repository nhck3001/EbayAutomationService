using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EbayAutomationService.Infrastructure;
public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        // Listing object is matched to listings table
        builder.ToTable("listings");
        // Set Id field as primary key
        builder.HasKey(x => x.Id);
        // Set Sku field to column sku
        builder.Property(x => x.listingId).HasColumnName("ListingId").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        // Create index for sku
        builder.HasIndex(x => x.listingId).IsUnique();
        // Create index for ebay_item_id
        builder.HasOne(x => x.Offer).WithOne()                                  
                .HasForeignKey<Listing>(l => l.OfferId)
                .OnDelete(DeleteBehavior.Cascade);    
    }
}
