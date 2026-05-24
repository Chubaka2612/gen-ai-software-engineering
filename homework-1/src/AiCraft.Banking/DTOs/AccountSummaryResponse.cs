namespace AiCraft.Banking.DTOs;

public class AccountSummaryResponse
{
    public string AccountId { get; set; } = string.Empty;
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public int TransactionCount { get; set; }
    public DateTimeOffset MostRecentTimestamp { get; set; }
}
