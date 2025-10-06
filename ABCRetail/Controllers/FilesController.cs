using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class FilesController : Controller
    {
        private readonly AzureFileShareService _files;
        public FilesController(AzureFileShareService files) => _files = files;


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _files.ListAsync();
            return View(list);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));
            var safeName = Path.GetFileName(file.FileName);
            await using var s = file.OpenReadStream();
            await _files.UploadAsync(safeName, s);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Delete(string name)
        {
            await _files.DeleteAsync(name);
            return RedirectToAction(nameof(Index));
        }
    }
}
