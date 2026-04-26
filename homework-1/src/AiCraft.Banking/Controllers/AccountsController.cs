using AiCraft.Banking.DTOs;
using AiCraft.Banking.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCraft.Banking.Controllers;

/// <summary>
/// Exposes per-account views: balance and summary.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AccountsController : ControllerBase
{
    private readonly ITransactionService _service;

    public AccountsController(ITransactionService service) => _service = service;

    /// <summary>
    /// Computes the net balance for an account from Completed transactions only.
    /// Returns 404 when the accountId has never appeared in any transaction.
    /// </summary>
    /// <param name="accountId">Account identifier in ACC-XXXXX format.</param>
    [HttpGet("{accountId}/balance")]
    [ProducesResponseType(typeof(AccountBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetBalance(string accountId)
    {
        var result = _service.GetAccountBalance(accountId);
        return result is null ? NotFound() : Ok(result);
    }
}
