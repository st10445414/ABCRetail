using System.Text;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers.Api
{
    [ApiController]
    [Route("api/blobs")]
    public sealed class BlobsApiController : ControllerBase
    {
        private readonly AzureBlobService _blobs;
        public BlobsApiController(AzureBlobService blobs) => _blobs = blobs;

        public sealed class BlobRequest { public string? Name { get; init; } public string? Text { get; init; } }

        [HttpPost] 
        public async Task<IActionResult> WriteBlobText([FromBody] BlobRequest body)
        {
            body ??= new BlobRequest { Text = "hello ABC" };
            var name = string.IsNullOrWhiteSpace(body.Name)
                ? $"abc_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.txt"
                : Path.GetFileName(body.Name);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(body.Text ?? "hello ABC"));
            await _blobs.UploadAsync(name, ms);
            return Ok($"Uploaded blob: {name}");
        }

        [HttpPost("seed5")]
        [HttpGet("seed5")] // POST|GET /api/blobs/seed5
        public async Task<IActionResult> SeedBlobs()
        {
            var tasks = Enumerable.Range(1, 5).Select(async i =>
            {
                var name = $"seed_{i}.txt";
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes($"ABC Seed Blob {i}"));
                await _blobs.UploadAsync(name, ms);
            });
            await Task.WhenAll(tasks);
            return Ok("Seeded 5 blobs.");
        }
    }
}
