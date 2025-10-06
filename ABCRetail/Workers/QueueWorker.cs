using ABCRetail.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Workers
{
    public sealed class QueueWorker : BackgroundService
    {
        private readonly ILogger<QueueWorker> _log;
        private readonly AzureQueueService _queue;

        public QueueWorker(ILogger<QueueWorker> log, AzureQueueService queue)
        {
            _log = log; _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var batch = await _queue.DequeueBatchAsync(16);
                    foreach (var msg in batch)
                    {
                        _log.LogInformation("Processed queue message: {Message}", msg);
                    }
                    if (batch.Count > 0) await _queue.MarkProcessedAsync(batch);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "QueueWorker failed");
                }
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
