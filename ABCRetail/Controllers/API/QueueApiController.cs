using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers.Api
{
    [ApiController]
    [Route("api/queue")]
    public sealed class QueueApiController : ControllerBase
    {
        private readonly AzureQueueService _queues;
        public QueueApiController(AzureQueueService queues) { _queues = queues; }

        public sealed record TxMsg(string ItemId, string Action, int Quantity);

        [HttpPost] 
        public async Task<IActionResult> Enqueue([FromBody] TxMsg? msg)
        {
            msg ??= new TxMsg("ABC-ITEM-1", "purchase", 1);
            await _queues.EnqueueAsync(new TransactionMessage
            {
                ItemId = msg.ItemId,
                Action = msg.Action,
                Quantity = msg.Quantity
            });
            return Ok("Message enqueued.");
        }

        [HttpPost("seed5")]
        [HttpGet("seed5")] 
        public async Task<IActionResult> SeedQueue()
        {
            var tasks = Enumerable.Range(1, 5).Select(i =>
                _queues.EnqueueAsync(new TransactionMessage
                {
                    ItemId = $"ABC-ITEM-{i}",
                    Action = i % 2 == 0 ? "restock" : "purchase",
                    Quantity = i
                }));
            await Task.WhenAll(tasks);
            return Ok("Seeded 5 queue messages.");
        }
    }
}
