namespace AiCraft.Banking.DTOs;

public class AccountBalanceResponse
{
    public string AccountId { get; set; } = string.Empty;
    public IReadOnlyList<CurrencyBalance> Balances { get; set; } = [];
}
