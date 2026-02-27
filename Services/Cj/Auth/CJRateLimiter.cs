public class CJRateLimiter
{
    // Semaphore lock to ensure 1 thead can call API at a time
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _nextAllowedTime = DateTime.MinValue;

    public async Task ExecuteWithCjRateLimitAsync(Func<Task> action)
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;

            if (now < _nextAllowedTime)
                await Task.Delay(_nextAllowedTime - now);

            _nextAllowedTime = DateTime.UtcNow.AddMilliseconds(1100);

            await action();
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
    
}