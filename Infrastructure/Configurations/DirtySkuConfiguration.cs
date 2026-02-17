using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EbayAutomationService.Infrastructure;
public class DirtySkuConfiguration : IEntityTypeConfiguration<DirtySku>
{
    public void Configure(EntityTypeBuilder<DirtySku> builder)
    {
        // Create table
        builder.ToTable("dirtySkus");
        // Set Id field as primary key
        builder.HasKey(x => x.Id);
        // Map .Pid field to 'pid' column 
        builder.Property(x => x.Sku).HasColumnName("sku").IsRequired();
        // Map .CategoryId field to 'category_id' column 
        builder.Property(x => x.CategoryId).HasColumnName("ebay_category_id").IsRequired();
        // Map .Processed field to 'processed' column 
        builder.Property(x => x.Processed).HasColumnName("processed").HasDefaultValue(false);
        // Create an index on .Pid // '[id]
        builder.HasIndex(x => x.Sku).IsUnique();
    }
}
