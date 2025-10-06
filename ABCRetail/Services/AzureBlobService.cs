using ABCRetail.Models;

namespace ABCRetail.Services
{
    public sealed class AzureBlobService
    {
        private readonly string _dir;
        public AzureBlobService(StorageOptions options)
        {
            _dir = Path.Combine(options.RootPath, options.BlobContainerName);
            Directory.CreateDirectory(_dir);
        }

        public Task<IReadOnlyList<BlobViewItem>> ListAsync()
        {
            var items = Directory.Exists(_dir)
                ? Directory.EnumerateFiles(_dir)
                    .Select(p => new FileInfo(p))
                    .OrderByDescending(fi => fi.LastWriteTimeUtc)
                    .Select(fi => new BlobViewItem(fi.Name, fi.Length))
                    .ToList()
                : new List<BlobViewItem>();
            return Task.FromResult((IReadOnlyList<BlobViewItem>)items);
        }

        public async Task UploadAsync(string name, Stream content, string contentType = "")
        {
            var path = Path.Combine(_dir, GetSafeName(name));
            await using var fs = File.Create(path);
            await content.CopyToAsync(fs);
        }

        public Task DeleteAsync(string name)
        {
            var path = Path.Combine(_dir, GetSafeName(name));
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        private static string GetSafeName(string name)
        {
            foreach (var ch in Path.GetInvalidFileNameChars()) name = name.Replace(ch, '_');
            return name;
        }
    }
}

