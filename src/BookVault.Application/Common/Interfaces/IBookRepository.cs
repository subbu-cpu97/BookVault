

using BookVault.Application.Common.Specifications;
using BookVault.Domain.Common;
using BookVault.Domain.Entities;

namespace BookVault.Application.Common.Interfaces;

// Interface defined in Application, implemented in Infrastructure
// Application layer says WHAT it needs — Infrastructure says HOW
// This is Dependency Inversion: high-level policy doesn't depend on low-level details
public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);
    // Old method — keep for backward compat with existing handlers
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct = default);

    // New paged method — uses specification
    Task<PagedResult<Book>> GetPagedAsync(
        ISpecification<Book> spec,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(Book book, CancellationToken ct = default);
    Task UpdateAsync(Book book, CancellationToken ct = default);
    Task DeleteAsync(Book book, CancellationToken ct = default);
    Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken ct = default);
}
