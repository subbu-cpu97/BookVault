namespace BookVault.Application.Common.Interfaces;

// Unit of Work pattern — groups multiple operations into one transaction
// Either all changes save, or none do (atomicity)
// Interview answer: "Why not just call SaveChanges in the repository?"
// Because one use case might touch multiple repositories —
// you want one commit at the end, not one per repository call.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}