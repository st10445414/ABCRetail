using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers.Api
{
    [ApiController]
    [Route("api/customers")]
    public sealed class CustomersApiController : ControllerBase
    {
        private readonly AzureTableService _table;
        public CustomersApiController(AzureTableService table) => _table = table;

        public sealed class CustomerDto
        {
            public string? FirstName { get; init; }
            public string? LastName { get; init; }
            public string Email { get; init; } = string.Empty;
            public string? Phone { get; init; }
        }

        [HttpPost] 
        public async Task<IActionResult> StoreCustomer([FromBody] CustomerDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Body must include at least 'email'.");
            await _table.AddAsync(new CustomerEntity
            {
                FirstName = dto.FirstName ?? "",
                LastName = dto.LastName ?? "",
                Email = dto.Email,
                Phone = dto.Phone ?? ""
            });
            return Ok($"Added {dto.Email}.");
        }

        [HttpPost("seed5")]
        [HttpGet("seed5")] 
        public async Task<IActionResult> SeedCustomers()
        {
            var tasks = Enumerable.Range(1, 5).Select(i => _table.AddAsync(new CustomerEntity
            {
                FirstName = $"Test{i}",
                LastName = "ABC",
                Email = $"test{i}@abc.example",
                Phone = $"+27-555-00{i:00}"
            }));
            await Task.WhenAll(tasks);
            return Ok("Seeded 5 customers.");
        }
    }
}
