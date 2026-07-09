using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Domain;

namespace Shared.Infrastructure;

public sealed class DomainEventsClearingInterceptor : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearEvents(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ClearEvents(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private static void ClearEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (IHasDomainEvents entity in context.ChangeTracker.Entries()
                     .Select(entry => entry.Entity)
                     .OfType<IHasDomainEvents>())
        {
            entity.ClearDomainEvents();
        }
    }
}
