namespace AiCraft.Banking.Models;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
}
