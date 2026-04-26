using AiCraft.Banking.Models;

namespace AiCraft.Banking.DTOs;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TransactionStatus Status { get; set; }
}
