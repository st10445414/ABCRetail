namespace ABCRetail.Models
{
    public sealed class StorageOptions
    {
        public string RootPath { get; init; } = string.Empty;
        public string BlobContainerName { get; init; } = "abc-media";
        public string FileShareName { get; init; } = "abc-files";
        public string TableFileName { get; init; } = "customers.json";
        public string QueueFileName { get; init; } = "queue.jsonl";
        public string ProcessedQueueFileName { get; init; } = "queue-processed.jsonl";
    }
}
