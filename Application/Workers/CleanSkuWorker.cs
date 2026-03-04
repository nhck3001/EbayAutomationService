using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

public class CleanSkuWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CleanSkuWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("CleanSkuWorker started.");

        try
        {
            // Worker will execute every 10 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<CleanSkuUseCase>();
                    await processor.ExecuteAsync();
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
        // Allow Ctrl+c to shut down gracefully
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception in CleanSkuWorker.");
        }
        finally
        {
            Log.Information("CleanSkuWorker stopping.");
        }
    }
}