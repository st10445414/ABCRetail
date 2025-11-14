using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    [Route("database")]
    public sealed class DatabaseController : Controller
    {
        private readonly IAzureSqlDatabase _db;
        public DatabaseController(IAzureSqlDatabase db) => _db = db;

        [HttpPost("init")]
        public async Task<IActionResult> Init()
        {
            await _db.EnsureSchemaAsync();
            return Ok("Schema ensured");
        }

        [HttpPost("add-customer")]
        public async Task<IActionResult> AddCustomer([FromBody] CustomerDto dto)
        {
            await _db.UpsertCustomerAsync(dto);
            return Ok(dto.CustomerId);
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromQuery] string email, [FromQuery] string password, [FromQuery] string role = "Customer")
        {
            await _db.CreateUserAsync(email, password);
            await _db.AssignRoleAsync(email, role);
            return Ok($"User {email} created with role {role}");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromQuery] string email, [FromQuery] string password)
        {
            var res = await _db.AuthenticateAsync(email, password);
            if (!res.Success) return Unauthorized(res.FailureReason);
            return Ok(new { Email = email, Roles = res.Roles });
        }
    }
}