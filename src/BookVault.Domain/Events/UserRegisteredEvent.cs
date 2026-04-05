namespace BookVault.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : IDomainEvent;
