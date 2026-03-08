using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EbayAutomationService.Infrastructure;

public class EbayOrderConfiguration : IEntityTypeConfiguration<EbayOrder>
{
    public void Configure(EntityTypeBuilder<EbayOrder> builder)
    {
        // Map to table
        builder.ToTable("orders");

        // Primary key
        builder.HasKey(x => x.Id);

        // Columns
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.EbayOrderId).HasColumnName("ebay_order_id").IsRequired();

        builder.Property(x => x.PurchaseDate).HasColumnName("purchase_date").IsRequired();

        builder.Property(x => x.BuyerUsername).HasColumnName("buyer_username");

        builder.Property(x => x.BuyerFullName).HasColumnName("buyer_full_name");

        builder.Property(x => x.AddressLine1).HasColumnName("address_line1");

        builder.Property(x => x.AddressLine2).HasColumnName("address_line2");

        builder.Property(x => x.City).HasColumnName("city");

        builder.Property(x => x.State).HasColumnName("state");

        builder.Property(x => x.PostalCode).HasColumnName("postal_code");

        builder.Property(x => x.Country).HasColumnName("country");

        builder.Property(x => x.Phone).HasColumnName("phone");

        builder.Property(x => x.Email).HasColumnName("email");

        builder.Property(x => x.OrderPaymentStatus).HasColumnName("order_payment_status");

        builder.Property(x => x.OrderFulfillmentStatus).HasColumnName("order_fulfillment_status");

        builder.Property(x => x.Status).HasColumnName("status").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Unique constraint
        builder.HasIndex(x => x.EbayOrderId).IsUnique();

        // Relationship: Order -> OrderItems
        builder.HasMany(x => x.OrderItems)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}