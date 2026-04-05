using BookVault.Application.Common.Interfaces;
using BookVault.Application.Common.Specifications;
using BookVault.Domain.Common;
using BookVault.Domain.Entities;
using BookVault.Infrastructure.Persistence;
using BookVault.Infrastructure.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BookVault.Infrastructure.Repositories;

public class AuthorRepository(BookVaultDbContext db) : IAuthorRepository
{
    public async Task<Author?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Authors
            .Include(a => a.Books)   // load books so BookCount and AuthorBookItem work
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Author>> GetAllAsync(CancellationToken ct = default) =>
        await db.Authors
            .Include(a => a.Books)
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync(ct);

    public async Task<PagedResult<Author>> GetPagedAsync(
        ISpecification<Author> spec,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var countQuery = SpecificationEvaluator<Author>
            .GetQuery(db.Authors.AsQueryable(), spec);
        var totalCount = await countQuery.CountAsync(ct);

        var dataQuery = SpecificationEvaluator<Author>
            .GetQuery(db.Authors.AsQueryable(), spec);
        var items = await dataQuery.ToListAsync(ct);

        return PagedResult<Author>.Create(items, totalCount, page, pageSize);
    }
    public async Task AddAsync(Author author, CancellationToken ct = default) =>
        await db.Authors.AddAsync(author, ct);
}
