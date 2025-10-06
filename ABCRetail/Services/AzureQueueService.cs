using System.Text.Json;
using ABCRetail.Models;

namespace ABCRetail.Services
{
    public sealed class AzureQueueService
    {
        private readonly string _queueFile;
        private readonly string _processedFile;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public AzureQueueService(StorageOptions options)
        {
            Directory.CreateDirectory(options.RootPath);
            _queueFile = Path.Combine(options.RootPath, options.QueueFileName);
            _processedFile = Path.Combine(options.RootPath, options.ProcessedQueueFileName);
            if (!File.Exists(_queueFile)) File.WriteAllText(_queueFile, string.Empty);
            if (!File.Exists(_processedFile)) File.WriteAllText(_processedFile, string.Empty);
        }

        public async Task EnqueueAsync(TransactionMessage msg)
        {
            var line = JsonSerializer.Serialize(msg, _json) + Environment.NewLine;
            await File.AppendAllTextAsync(_queueFile, line);
        }

        public async Task<IReadOnlyList<string>> PeekAsync(int max = 32)
        {
            var lines = await File.ReadAllLinesAsync(_queueFile);
            return lines.Reverse().Take(max).Reverse().ToList().AsReadOnly();
        }

        // Used by QueueWorker
        public async Task<IReadOnlyList<string>> DequeueBatchAsync(int max = 16)
        {
            var lines = (await File.ReadAllLinesAsync(_queueFile)).ToList();
            var take = Math.Min(max, lines.Count);
            var batch = lines.Take(take).ToList();
            var remaining = lines.Skip(take).ToList();
            await File.WriteAllLinesAsync(_queueFile, remaining);
            return batch.AsReadOnly();
        }

        public async Task MarkProcessedAsync(IEnumerable<string> messages)
        {
            if (!messages.Any()) return;
            await File.AppendAllLinesAsync(_processedFile, messages);
        }
    }
}

