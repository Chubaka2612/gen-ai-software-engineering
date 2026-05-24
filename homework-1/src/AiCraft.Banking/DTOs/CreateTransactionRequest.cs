using AiCraft.Banking.Models;

namespace AiCraft.Banking.DTOs;

public class CreateTransactionRequest
{
    public string? FromAccount { get; set; }
    public string? ToAccount { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public TransactionType Type { get; set; }
}
