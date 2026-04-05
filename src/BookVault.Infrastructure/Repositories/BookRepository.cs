using BookVault.Application.Common.Interfaces;
using BookVault.Application.Common.Specifications;
using BookVault.Domain.Common;
using BookVault.Domain.Entities;
using BookVault.Infrastructure.Persistence;
using BookVault.Infrastructure.Persistence.Specifications;
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

    public async Task<PagedResult<Book>> GetPagedAsync(
        ISpecification<Book> spec,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        // Step 1 — Apply spec WITHOUT paging to get total count
        // Interview answer: "Why two queries for pagination?"
        // The COUNT query runs on all matching rows (after filter, before SKIP/TAKE).
        // The data query runs with SKIP/TAKE. You can't get the total from a
        // paged query — you'd only know the current page's count.
        // EF Core is smart: it translates COUNT(*) with the same WHERE clause.
        var countQuery = SpecificationEvaluator<Book>
            .GetQuery(db.Books.AsQueryable(), spec);

        // Must count BEFORE applying Skip/Take
        var totalCount = await countQuery.CountAsync(ct);

        // Step 2 — Apply spec WITH paging to get the current page's rows
        var dataQuery = SpecificationEvaluator<Book>
            .GetQuery(db.Books.AsQueryable(), spec);

        var items = await dataQuery.ToListAsync(ct);

        return PagedResult<Book>.Create(items, totalCount, page, pageSize);
    }

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
