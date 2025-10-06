using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class BlobsController : Controller
    {
        private readonly AzureBlobService _blobs;
        public BlobsController(AzureBlobService blobs) => _blobs = blobs;


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _blobs.ListAsync();
            return View(list);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));
            var safeName = Path.GetFileName(file.FileName);
            await using var s = file.OpenReadStream();
            await _blobs.UploadAsync(safeName, s, file.ContentType);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Delete(string name)
        {
            await _blobs.DeleteAsync(name);
            return RedirectToAction(nameof(Index));
        }
    }
}
