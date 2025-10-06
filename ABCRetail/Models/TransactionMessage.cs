namespace ABCRetail.Models
{
    public sealed class TransactionMessage
    {
        public required string ItemId { get; init; }
        public required string Action { get; init; } 
        public int Quantity { get; init; }
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public string Source { get; init; } = "web"; 
    }
}
