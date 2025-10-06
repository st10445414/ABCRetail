namespace ABCRetail.Models
{
    public class CustomerEntity
    {
        public string PartitionKey { get; set; } = "ABC";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("n");
        public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
