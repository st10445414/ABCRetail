using ABCRetail.Models;
using ABCRetail.Services;
using ABCRetail.Workers;

var builder = WebApplication.CreateBuilder(args);

var contentRoot = builder.Environment.ContentRootPath;
var webRoot = builder.Environment.WebRootPath ?? Path.Combine(contentRoot, "wwwroot");
var storageRoot = Path.Combine(webRoot, "storage");
Directory.CreateDirectory(webRoot);
Directory.CreateDirectory(storageRoot);

var options = new StorageOptions
{
    RootPath = storageRoot,
    BlobContainerName = "abc-media",
    FileShareName = "abc-files",
    TableFileName = "customers.json",
    QueueFileName = "queue.jsonl",
    ProcessedQueueFileName = "queue-processed.jsonl"
};
builder.Services.AddSingleton(options);

builder.Services.AddSingleton<AzureBlobService>();
builder.Services.AddSingleton<AzureFileShareService>();
builder.Services.AddSingleton<AzureQueueService>();
builder.Services.AddSingleton<AzureTableService>();

builder.Services.AddHostedService<QueueWorker>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapDefaultControllerRoute();
app.MapControllers();

app.Run();
