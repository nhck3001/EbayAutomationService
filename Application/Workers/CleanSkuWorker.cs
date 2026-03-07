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
            // Worker will execute every 5 second
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<CleanSkuUseCase>();
                    await processor.ProcessBatchAsync(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        catch (CjDailyLimitException)
        {
            Log.Warning("Daily limit reached. Stopping CleanSkuWorker.");
            return; // exit worker cleanly
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