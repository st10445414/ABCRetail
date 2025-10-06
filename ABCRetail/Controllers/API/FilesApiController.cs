using System.Text;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers.Api
{
    [ApiController]
    [Route("api/files")]
    public sealed class FilesApiController : ControllerBase
    {
        private readonly AzureFileShareService _files;
        public FilesApiController(AzureFileShareService files) => _files = files;

        public sealed class FileRequest { public string? Name { get; init; } public string? Text { get; init; } }

        [HttpPost] 
        public async Task<IActionResult> WriteFile([FromBody] FileRequest body)
        {
            body ??= new FileRequest { Text = "ABC file" };
            var name = string.IsNullOrWhiteSpace(body.Name)
                ? $"abc_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.txt"
                : Path.GetFileName(body.Name);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(body.Text ?? "ABC file"));
            await _files.UploadAsync(name, ms);
            return Ok($"Uploaded file: {name}");
        }

        [HttpPost("seed5")]
        [HttpGet("seed5")] 
        public async Task<IActionResult> SeedFiles()
        {
            var tasks = Enumerable.Range(1, 5).Select(async i =>
            {
                var name = $"seed_{i}.txt";
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes($"ABC File {i}"));
                await _files.UploadAsync(name, ms);
            });
            await Task.WhenAll(tasks);
            return Ok("Seeded 5 files.");
        }
    }
}
