using FluentValidation;

namespace CashFlow.Transactions.Application.Commands.RegisterTransaction;

public sealed class RegisterTransactionValidator : AbstractValidator<RegisterTransactionCommand>
{
    public RegisterTransactionValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.OccurredOnUtc).LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(1));
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
