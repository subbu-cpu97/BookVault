namespace BookVault.Domain.Events;

public record BookUpdatedEvent(Guid BookId, string Title) : IDomainEvent;