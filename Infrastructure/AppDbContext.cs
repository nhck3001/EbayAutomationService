using EbayAutomationService.Infrastructure;
using Microsoft.EntityFrameworkCore;

// This class acts as a bridge between C# code and database
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    // Each DbSet<T> is a database table that can be queries
    // The => Set<T>() is a shorthand that returns the DbSet for that entity type.
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductPid> ProductPids => Set<ProductPid>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<Listing> Listings => Set<Listing>();

    // This function applies all the configuration that has been built 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductPidConfiguration());
        modelBuilder.ApplyConfiguration(new SkuConfiguration());
        modelBuilder.ApplyConfiguration(new ListingConfiguration());
    }
}
