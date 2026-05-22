namespace CashFlow.Transactions.Domain.Common;

/// <summary>
/// Base for DDD aggregates. Holds a list of domain events that will be
/// dispatched after the persistence transaction commits.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
