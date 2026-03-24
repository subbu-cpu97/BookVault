using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using BookVault.Infrastructure.Persistence;
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

    public async Task AddAsync(Author author, CancellationToken ct = default) =>
        await db.Authors.AddAsync(author, ct);
}