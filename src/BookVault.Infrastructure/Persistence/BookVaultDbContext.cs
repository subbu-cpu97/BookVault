using BookVault.Application.Common.Interfaces;
using BookVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookVault.Infrastructure.Persistence;

// DbContext = EF Core's Unit of Work + session with the database
// Also implements IUnitOfWork — one SaveChangesAsync commits everything
public class BookVaultDbContext(DbContextOptions<BookVaultDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Scans this assembly for all IEntityTypeConfiguration<T> classes
        // and applies them — keeps DbContext clean, one config file per entity
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookVaultDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
