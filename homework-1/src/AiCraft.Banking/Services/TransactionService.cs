using System.Collections.Concurrent;
using AiCraft.Banking.DTOs;
using AiCraft.Banking.Models;

namespace AiCraft.Banking.Services;

/// <summary>
/// Singleton service owning the in-memory transaction store.
/// Uses ConcurrentDictionary for lock-free reads and O(1) lookup by Id.
/// </summary>
public class TransactionService : ITransactionService
{
    // Keyed by Id — ConcurrentDictionary has no insertion-order guarantee,
    // so listing always sorts by Timestamp.
    private readonly ConcurrentDictionary<Guid, Transaction> _transactions = [];

    /// <inheritdoc />
    public Transaction CreateTransaction(CreateTransactionRequest request)
    {
        var transaction = new Transaction
        {
            FromAccount = request.FromAccount ?? string.Empty,
            ToAccount = request.ToAccount ?? string.Empty,
            Amount = Math.Round(request.Amount, 2),          // normalise on ingestion to absorb JSON float noise
            Currency = request.Currency?.ToUpperInvariant() ?? string.Empty,
            Type = request.Type
        };

        _transactions.TryAdd(transaction.Id, transaction);

        return transaction;
    }

    /// <inheritdoc />
    public IEnumerable<Transaction> GetTransactions(TransactionFilter filter)
    {
        // Sort by Timestamp to compensate for ConcurrentDictionary's unordered enumeration.
        IEnumerable<Transaction> query = _transactions.Values.OrderBy(t => t.Timestamp);

        if (filter.AccountId is not null)
            query = query.Where(t =>
                t.FromAccount == filter.AccountId ||
                t.ToAccount == filter.AccountId);

        if (filter.Type is not null)
            query = query.Where(t => t.Type == filter.Type);

        if (filter.From is not null)
            query = query.Where(t => t.Timestamp >= filter.From);

        if (filter.To is not null)
            query = query.Where(t => t.Timestamp <= filter.To);

        return query.ToList();
    }

    /// <inheritdoc />
    public Transaction? GetTransactionById(Guid id)
    {
        _transactions.TryGetValue(id, out var transaction);
        return transaction;
    }

    /// <inheritdoc />
    public AccountBalanceResponse? GetAccountBalance(string accountId)
    {
        var all = _transactions.Values
            .Where(t => t.FromAccount == accountId || t.ToAccount == accountId)
            .ToList();

        // Null signals the account is unknown, not that the balance is zero.
        if (all.Count == 0)
            return null;

        // Only Completed transactions affect the settled balance; Pending/Failed are excluded.
        var balance = all
            .Where(t => t.Status == TransactionStatus.Completed)
            .Sum(t => t.ToAccount == accountId ? t.Amount : -t.Amount);

        return new AccountBalanceResponse
        {
            AccountId = accountId,
            Balance = balance
        };
    }

    /// <inheritdoc />
    public AccountSummaryResponse? GetAccountSummary(string accountId)
    {
        var all = _transactions.Values
            .Where(t => t.FromAccount == accountId || t.ToAccount == accountId)
            .ToList();

        // Null signals the account is unknown, not that it has no activity.
        if (all.Count == 0)
            return null;

        // Summary intentionally includes all statuses — it reflects transaction history,
        // not settled funds (that is GetAccountBalance's concern).
        return new AccountSummaryResponse
        {
            AccountId = accountId,
            TotalDeposits = all.Where(t => t.ToAccount == accountId).Sum(t => t.Amount),
            TotalWithdrawals = all.Where(t => t.FromAccount == accountId).Sum(t => t.Amount),
            TransactionCount = all.Count,
            MostRecentTimestamp = all.Max(t => t.Timestamp)
        };
    }
}
