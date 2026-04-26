using AiCraft.Banking.DTOs;
using AiCraft.Banking.Models;

namespace AiCraft.Banking.Services;

/// <summary>
/// Manages the in-memory transaction store and all business logic.
/// All method implementations must be thread-safe.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Creates and persists a new transaction with server-generated Id and Timestamp.
    /// Status is always set to Pending on creation.
    /// </summary>
    Transaction CreateTransaction(CreateTransactionRequest request);

    /// <summary>
    /// Returns all transactions matching the given filter.
    /// All filter fields are optional and combinable; an empty filter returns every transaction.
    /// Returns an empty collection (not null) when nothing matches.
    /// </summary>
    IEnumerable<Transaction> GetTransactions(TransactionFilter filter);

    /// <summary>
    /// Returns the transaction with the given Id, or null if it does not exist.
    /// </summary>
    Transaction? GetTransactionById(Guid id);

    /// <summary>
    /// Computes the net balance for an account from Completed transactions only.
    /// Returns null when the accountId has never appeared in any transaction,
    /// distinguishing "account unknown" from "account with a zero balance".
    /// </summary>
    AccountBalanceResponse? GetAccountBalance(string accountId);

    /// <summary>
    /// Returns aggregate stats across all transactions (any status) for an account.
    /// Returns null when the accountId has never appeared in any transaction.
    /// </summary>
    AccountSummaryResponse? GetAccountSummary(string accountId);
}
