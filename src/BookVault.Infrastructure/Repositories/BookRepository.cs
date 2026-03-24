using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using BookVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookVault.Infrastructure.Repositories;

// Implements the interface defined in Application layer
// Concrete class — knows about EF Core (infrastructure detail)
// Interview answer: "Why the Repository pattern over direct DbContext?"
// Repositories abstract the data access layer. Your handlers call
// IBookRepository, not DbContext directly. Swap Postgres for MongoDB?
// Change one class. Unit test handlers? Mock the interface — no real DB needed.
public class BookRepository(BookVaultDbContext db) : IBookRepository
{
    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Books
            .Include(b => b.Author)  // eager load — one query with JOIN
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct = default) =>
        await db.Books
            .Include(b => b.Author)
            .OrderBy(b => b.Title)
            .ToListAsync(ct);

    public async Task AddAsync(Book book, CancellationToken ct = default) =>
        await db.Books.AddAsync(book, ct);

    public Task UpdateAsync(Book book, CancellationToken ct = default)
    {
        db.Books.Update(book);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Book book, CancellationToken ct = default)
    {
        db.Books.Remove(book);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken ct = default) =>
        await db.Books.AnyAsync(b => b.ISBN == isbn, ct);
}