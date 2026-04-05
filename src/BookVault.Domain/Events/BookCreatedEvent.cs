namespace BookVault.Domain.Events;

public record BookCreatedEvent(Guid BookId, string Title, Guid AuthorId) : IDomainEvent;
