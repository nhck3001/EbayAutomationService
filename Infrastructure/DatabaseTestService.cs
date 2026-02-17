using Microsoft.EntityFrameworkCore;

namespace EbayAutomationService.Infrastructure;

public class DatabaseTestService
{
    private readonly AppDbContext _context;
    public DatabaseTestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Testing database connection");
        var canConnect = await _context.Database.CanConnectAsync();
        if (!canConnect)
        {
            Console.WriteLine("Can not connect to the database");
            return;
        }
        Console.WriteLine("Connect Successfully");

        // Test inserting a row to the database
        _context.Categories.Add(new Category { EbayCategoryName = "Test Category", EbayCategoryId = "12345", Keyword = "test" });
        await _context.SaveChangesAsync();
        Console.WriteLine("Test category inserted.");
        var count = await _context.Categories.CountAsync();
        Console.WriteLine($"There's {count} row in categories table");
    }
}
