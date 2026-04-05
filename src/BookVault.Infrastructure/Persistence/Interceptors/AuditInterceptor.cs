using BookVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BookVault.Infrastructure.Persistence.Interceptors;

// EF Core Interceptor — hooks into SaveChanges pipeline
// Interview answer: "What is the Interceptor pattern?"
// Intercept a call and add cross-cutting behavior without modifying the original.
// Here we intercept every save and automatically stamp timestamps —
// developers never need to set CreatedOn/UpdatedOn manually, ever.
public class AuditInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        SetAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void SetAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.UtcNow;  // always UTC in the database — convert to local in UI

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn = now;
                entry.Entity.UpdatedOn = now;
                entry.Entity.CreatedBy = "system";  // in a real app, inject IUserService to get the actual user
                entry.Entity.UpdatedBy = "";
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedOn = now;
                entry.Entity.UpdatedBy = "system";
            }
        }
    }
}
