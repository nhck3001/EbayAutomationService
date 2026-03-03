using Microsoft.Extensions.Hosting;

public class InventoryCreationWorker : BackgroundService
{
    private readonly CreateInventoryUseCase _processor;

    public InventoryCreationWorker(CreateInventoryUseCase processor)
    {
        _processor = processor;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _processor.ProcessBatchAsync();
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}