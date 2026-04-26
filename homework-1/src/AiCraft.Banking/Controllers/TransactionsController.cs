using AiCraft.Banking.DTOs;
using AiCraft.Banking.Models;
using AiCraft.Banking.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCraft.Banking.Controllers;

/// <summary>
/// Manages transaction resources: create, list, and retrieve by ID.
/// </summary>
[ApiController]
[Route("[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service) => _service = service;

    /// <summary>
    /// Returns all transactions, optionally filtered by account, type, or date range.
    /// All query parameters are optional and combinable.
    /// Returns an empty array when nothing matches — never 404.
    /// </summary>
    /// <param name="filter">Optional filters: accountId, type, from, to.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAll([FromQuery] TransactionFilter filter)
    {
        var result = _service.GetTransactions(filter).Select(MapToResponse);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single transaction by its ID.
    /// Returns 404 if no transaction with the given ID exists.
    /// Returns 404 for non-Guid paths — the route constraint prevents matching entirely.
    /// </summary>
    /// <param name="id">The transaction Guid.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var transaction = _service.GetTransactionById(id);
        return transaction is null ? NotFound() : Ok(MapToResponse(transaction));
    }

    /// <summary>
    /// Creates a new transaction. Id, Timestamp, and Status are set server-side.
    /// Returns 201 with a Location header pointing to GET /transactions/{id}.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    public IActionResult Create([FromBody] CreateTransactionRequest request)
    {
        var transaction = _service.CreateTransaction(request);
        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, MapToResponse(transaction));
    }

    // Shared by all actions — maps the internal domain model to the public DTO.
    internal static TransactionResponse MapToResponse(Transaction t) => new()
    {
        Id = t.Id,
        FromAccount = t.FromAccount,
        ToAccount = t.ToAccount,
        Amount = t.Amount,
        Currency = t.Currency,
        Type = t.Type,
        Timestamp = t.Timestamp,
        Status = t.Status
    };
}
