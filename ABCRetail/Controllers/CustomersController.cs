using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class CustomersController : Controller
    {
        private readonly AzureTableService _table;
        public CustomersController(AzureTableService table) => _table = table;


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customers = await _table.ListAsync(100);
            return View(customers);
        }


        [HttpGet]
        public IActionResult Create() => View(new CustomerEntity());


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerEntity model)
        {
            if (!ModelState.IsValid) return View(model);
            model.PartitionKey = "ABC"; 
            model.RowKey = string.IsNullOrWhiteSpace(model.RowKey) ? Guid.NewGuid().ToString("n") : model.RowKey;
            await _table.AddAsync(model);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Seed5()
        {
            var tasks = Enumerable.Range(1, 5).Select(i => _table.AddAsync(new CustomerEntity
            {
                FirstName = $"Test{i}",
                LastName = "ABC",
                Email = $"test{i}@abc.example",
                Phone = $"+31-555-00{i:00}"
            }));
            await Task.WhenAll(tasks);
            return RedirectToAction(nameof(Index));
        }
    }
}
