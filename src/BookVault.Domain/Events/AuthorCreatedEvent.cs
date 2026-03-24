namespace BookVault.Domain.Events;

// Record = immutable data carrier — perfect for events (they never change)
// This is the Event pattern — something that already happened
public record AuthorCreatedEvent(Guid AuthorId, string FullName) : IDomainEvent;