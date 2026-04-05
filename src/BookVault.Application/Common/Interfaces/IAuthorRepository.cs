using BookVault.Application.Common.Specifications;
using BookVault.Domain.Common;
using BookVault.Domain.Entities;

namespace BookVault.Application.Common.Interfaces;

public interface IAuthorRepository
{
    Task<Author?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Author>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<Author>> GetPagedAsync(
        ISpecification<Author> spec,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(Author author, CancellationToken ct = default);
}
