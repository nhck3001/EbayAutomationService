using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EbayAutomationService.Infrastructure;
public class ProductPidConfiguration : IEntityTypeConfiguration<ProductPid>
{
    public void Configure(EntityTypeBuilder<ProductPid> builder)
    {
        // Create table
        builder.ToTable("product_pids");
        // Set Id field as primary key
        builder.HasKey(x => x.Id);
        // Map .Pid field to 'pid' column 
        builder.Property(x => x.Pid).HasColumnName("pid").IsRequired();
        // Map .CategoryId field to 'category_id' column 
        builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        // Map .Processed field to 'processed' column 
        builder.Property(x => x.Processed).HasColumnName("processed").HasDefaultValue(false);
        // Create an index on .Pid // '[id]
        builder.HasIndex(x => x.Pid).IsUnique();
    }
}
