namespace CashFlow.Transactions.Domain.Exceptions;

/// <summary>
/// Signals a domain invariant violation.
/// Mapped to HTTP 422 Unprocessable Entity by the API layer.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
