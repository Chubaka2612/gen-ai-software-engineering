using System.Text.RegularExpressions;
using AiCraft.Banking.DTOs;
using AiCraft.Banking.Models;
using FluentValidation;

namespace AiCraft.Banking.Validators;

/// <summary>
/// Validates <see cref="CreateTransactionRequest"/> according to the banking domain rules.
/// Registered via FluentValidation auto-validation so the controller never receives an invalid model.
/// </summary>
public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    private static readonly Regex AccountPattern =
        new(@"^ACC-[A-Z0-9]{5}$", RegexOptions.Compiled);

    private static readonly string[] SupportedCurrencies =
        ["AUD", "CAD", "CHF", "CNY", "EUR", "GBP", "JPY", "USD"];

    public CreateTransactionRequestValidator()
    {
        AddAmountRules();
        AddCurrencyRules();
        AddFromAccountRules();
        AddToAccountRules();
        AddTransferRules();
    }

    private void AddAmountRules() =>
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.")
            .Must(a => a == Math.Round(a, 2))
            .WithMessage("Amount must have at most 2 decimal places.");

    private void AddCurrencyRules() =>
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Must(c => c != null && SupportedCurrencies.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}.");

    // FromAccount is required for Withdrawal and Transfer; optional (but format-checked) for Deposit.
    private void AddFromAccountRules()
    {
        When(x => x.Type is TransactionType.Withdrawal or TransactionType.Transfer, () =>
        {
            RuleFor(x => x.FromAccount)
                .NotEmpty().WithMessage("FromAccount is required for this transaction type.")
                .Matches(AccountPattern).WithMessage("FromAccount must match the format ACC-XXXXX.");
        }).Otherwise(() =>
        {
            RuleFor(x => x.FromAccount)
                .Matches(AccountPattern).When(x => !string.IsNullOrWhiteSpace(x.FromAccount))
                .WithMessage("FromAccount must match the format ACC-XXXXX.");
        });
    }

    // ToAccount is required for Deposit and Transfer; optional (but format-checked) for Withdrawal.
    private void AddToAccountRules()
    {
        When(x => x.Type is TransactionType.Deposit or TransactionType.Transfer, () =>
        {
            RuleFor(x => x.ToAccount)
                .NotEmpty().WithMessage("ToAccount is required for this transaction type.")
                .Matches(AccountPattern).WithMessage("ToAccount must match the format ACC-XXXXX.");
        }).Otherwise(() =>
        {
            RuleFor(x => x.ToAccount)
                .Matches(AccountPattern).When(x => !string.IsNullOrWhiteSpace(x.ToAccount))
                .WithMessage("ToAccount must match the format ACC-XXXXX.");
        });
    }

    private void AddTransferRules() =>
        When(x => x.Type == TransactionType.Transfer, () =>
        {
            RuleFor(x => x.FromAccount)
                .NotEqual(x => x.ToAccount)
                .WithMessage("FromAccount and ToAccount must not be the same for a Transfer.");
        });
}
