using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EbayAutomationService.Infrastructure;

public class LineItemConfiguration : IEntityTypeConfiguration<LineItem>
{
    public void Configure(EntityTypeBuilder<LineItem> builder)
    {
        // Map to table
        builder.ToTable("line_items");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.LineItemId)
            .HasColumnName("line_item_id")
            .IsRequired();

        builder.Property(x => x.Sku)
            .HasColumnName("sku")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(x => x.CjOrderId)
            .HasColumnName("cj_order_id");

        builder.Property(x => x.TrackingNumber)
            .HasColumnName("tracking_number");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        // Prevent duplicate line items
        builder.HasIndex(x => x.LineItemId)
            .IsUnique();
    }
}