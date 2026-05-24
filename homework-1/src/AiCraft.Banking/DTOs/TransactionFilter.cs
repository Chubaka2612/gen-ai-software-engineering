using AiCraft.Banking.Models;

namespace AiCraft.Banking.DTOs;

public class TransactionFilter
{
    public string? AccountId { get; set; }
    public TransactionType? Type { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
