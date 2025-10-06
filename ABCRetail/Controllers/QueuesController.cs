using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class QueuesController : Controller
    {
        private readonly AzureQueueService _queue;
        public QueuesController(AzureQueueService queue) => _queue = queue;


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var messages = await _queue.PeekAsync(32);
            return View(messages);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enqueue(string itemId, string action, int quantity)
        {
            var msg = new TransactionMessage { ItemId = itemId, Action = action, Quantity = quantity };
            await _queue.EnqueueAsync(msg);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Seed5()
        {
            var now = DateTime.UtcNow; 
            var tasks = Enumerable.Range(1, 5).Select(i => _queue.EnqueueAsync(new TransactionMessage
            {
                ItemId = $"ABC-ITEM-{i}",
                Action = i % 2 == 0 ? "restock" : "purchase",
                Quantity = i * 2,
                TimestampUtc = now.AddSeconds(i)
            }));
            await Task.WhenAll(tasks);
            return RedirectToAction(nameof(Index));
        }
    }
}
