using BookVault.Domain.Events;

namespace BookVault.Domain.Entities
{
    // Abstract = cannot be instantiated directly, only inherited
    // This is the Template Method pattern — defines structure, subclasses fill in details
    public abstract class BaseEntity
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        // These are set automatically by the Audit Interceptor — never manually
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        

        // Domain events — things that happened in the domain
        // Private backing field — encapsulation (OOP principle)

        private List<IDomainEvent> _domainEvents = new List<IDomainEvent>();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

         public void ClearDomainEvents() => _domainEvents.Clear();

    }
}