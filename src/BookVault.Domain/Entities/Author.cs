using BookVault.Domain.Events;

namespace BookVault.Domain.Entities
{
    public class Author : BaseEntity
    {
        // Private setters — only this class can change its own state (Encapsulation)
        public string FirstName { get; private set; } = string.Empty;
        public string LastName  { get; private set; } = string.Empty;
        public string Bio       { get; private set; } = string.Empty;

        // Navigation property — EF Core uses this for JOINs
        public ICollection<Book> Books { get; private set; } = [];

        // Computed property — no storage in DB, derived from other fields
        public string FullName => $"{FirstName} {LastName}";

        // Private constructor — forces use of factory method (Factory pattern)
        private Author() { }

        // Factory method — single place where Authors are created, validates inputs
        public static Author Create(string firstName, string lastName, string bio)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
            ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

            var author = new Author
            {
                FirstName = firstName.Trim(),
                LastName  = lastName.Trim(),
                Bio       = bio?.Trim() ?? string.Empty
            };

            // Raise a domain event — something meaningful happened in the domain
            author.RaiseDomainEvent(new AuthorCreatedEvent(author.Id, author.FullName));
            return author;
        }

        public void Update(string firstName, string lastName, string bio)
        {
            FirstName = firstName.Trim();
            LastName  = lastName.Trim();
            Bio       = bio?.Trim() ?? string.Empty;
        }
    }
}