using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EbayAutomation.Model.Database;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Map Category class to 'categories' table
        builder.ToTable("categories");
        // Set id as the primary key
        builder.HasKey(categoryObject => categoryObject.Id);
        // Map EbayCategoryName field to column ebay_category_name. ebay_category_name can't be NULL
        builder.Property(categoryObject => categoryObject.EbayCategoryName).HasColumnName("ebay_category_name").IsRequired();
        // Map EbayCategoryId field to column ebay_category_id. ebay_category_id can't be NULL
        builder.Property(categoryObject => categoryObject.EbayCategoryId).HasColumnName("ebay_category_id").IsRequired();
        // Map Keyword field to column keyword. keyword can't be NULL
        builder.Property(categoryObject => categoryObject.Keyword).HasColumnName("keyword").IsRequired();
        // Create unique index of ebay_category_id
        builder.HasIndex(categoryObject=> categoryObject.EbayCategoryId).IsUnique();
        // Relationship between Category and ProductPids is 1 to many
        // Each product Pid belong to 1 Category
        // Foreign key is CategoryId in ProductPids table
        // If a category is deleted, all of related Pids are also deleted
        builder.HasMany(x => x.ProductPids).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
