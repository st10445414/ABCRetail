using ABCRetail.Models;

namespace ABCRetail.Services
{
   
    public sealed class AzureFileShareService
    {
        private readonly string _dir;
        public AzureFileShareService(StorageOptions options)
        {
            _dir = Path.Combine(options.RootPath, options.FileShareName);
            Directory.CreateDirectory(_dir);
        }

        public Task<IReadOnlyList<FileViewItem>> ListAsync()
        {
            var list = Directory.EnumerateFileSystemEntries(_dir)
                .Select(p => new FileViewItem(Path.GetFileName(p)!, Directory.Exists(p)))
                .OrderBy(f => f.IsDirectory ? 0 : 1)
                .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult((IReadOnlyList<FileViewItem>)list);
        }

        public async Task UploadAsync(string name, Stream content)
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

