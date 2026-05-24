using System.Text.Json;
using System.Text.Json.Serialization;
using AiCraft.Banking.Models;

namespace AiCraft.Banking.Services;

/// <summary>
/// Hosted service that loads sample-data.json into the transaction store on startup.
/// Runs before the first request; failures are logged and skipped, never fatal.
/// </summary>
internal sealed class DataSeeder : IHostedService
{
    private readonly TransactionService _service;
    private readonly ILogger<DataSeeder> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public DataSeeder(TransactionService service, ILogger<DataSeeder> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "sample-data.json");

        if (!File.Exists(path))
        {
            _logger.LogWarning("sample-data.json not found at {Path}. Skipping seed.", path);
            return;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var transactions = JsonSerializer.Deserialize<List<Transaction>>(json, JsonOptions);

        if (transactions is null or { Count: 0 })
        {
            _logger.LogWarning("sample-data.json is empty or could not be parsed.");
            return;
        }

        _service.Seed(transactions);
        _logger.LogInformation("Seeded {Count} transactions from sample-data.json.", transactions.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
