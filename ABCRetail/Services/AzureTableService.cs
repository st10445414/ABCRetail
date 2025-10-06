using System.Text.Json;
using ABCRetail.Models;

namespace ABCRetail.Services
{
    public sealed class AzureTableService
    {
        private readonly string _path;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

        public AzureTableService(StorageOptions options)
        {
            Directory.CreateDirectory(options.RootPath);
            _path = Path.Combine(options.RootPath, options.TableFileName);
            if (!File.Exists(_path)) File.WriteAllText(_path, "[]");
        }

        public async Task AddAsync(CustomerEntity entity)
        {
            var list = await LoadAsync();
            entity.PartitionKey = string.IsNullOrWhiteSpace(entity.PartitionKey) ? "ABC" : entity.PartitionKey;
            entity.RowKey = string.IsNullOrWhiteSpace(entity.RowKey) ? Guid.NewGuid().ToString("n") : entity.RowKey;
            list.Add(entity);
            await SaveAsync(list);
        }

        public async Task<IReadOnlyList<CustomerEntity>> ListAsync(int take = 50)
        {
            var list = await LoadAsync();
            return list.Where(c => c.PartitionKey == "ABC").Take(take).ToList().AsReadOnly();
        }

        private async Task<List<CustomerEntity>> LoadAsync()
        {
            using var fs = File.OpenRead(_path);
            List<CustomerEntity>? items = await JsonSerializer.DeserializeAsync<List<CustomerEntity>>(fs, _json);
            return items ?? new List<CustomerEntity>();
        }

        private Task SaveAsync(List<CustomerEntity> list)
        {
            var json = JsonSerializer.Serialize(list, _json);
            return File.WriteAllTextAsync(_path, json);
        }
    }
}


