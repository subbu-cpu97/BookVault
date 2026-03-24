

using BookVault.Domain.Entities;

namespace BookVault.Application.Common.Interfaces;

// Interface defined in Application, implemented in Infrastructure
// Application layer says WHAT it needs — Infrastructure says HOW
// This is Dependency Inversion: high-level policy doesn't depend on low-level details
public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Book book, CancellationToken ct = default);
    Task UpdateAsync(Book book, CancellationToken ct = default);
    Task DeleteAsync(Book book, CancellationToken ct = default);
    Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken ct = default);
}