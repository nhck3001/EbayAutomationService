using Microsoft.Extensions.Hosting;
using Serilog;

public class CreateOfferWorker : BackgroundService
{
    private readonly CreateOfferUseCase _processor;

    public CreateOfferWorker(CreateOfferUseCase processor)
    {
        _processor = processor;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("CreateOfferWorker started.");

        try
        {
            // Worker will execute every 10 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                await _processor.ProcessBatchAsync();
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
            Log.Error(ex, "Unhandled exception in CreateOfferWorker.");
        }
        finally
        {
            Log.Information("CreateOfferWorker stopping.");
        }
    }
}