using System;

namespace ABCRetail.Models
{
    public sealed class CustomerDto
    {
        public Guid CustomerId { get; set; } = Guid.NewGuid();
        public string PartitionKey { get; set; } = "ABC";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class AuthResult
    {
        public bool Success { get; init; }
        public string[] Roles { get; init; } = Array.Empty<string>();
        public string? FailureReason { get; init; }
    }
}
