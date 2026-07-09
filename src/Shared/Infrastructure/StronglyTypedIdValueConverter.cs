using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shared.Domain;

namespace Shared.Infrastructure;

public sealed class StronglyTypedIdValueConverter<TId> : ValueConverter<TId, Guid>
    where TId : IStronglyTypedId<TId>
{
    public StronglyTypedIdValueConverter()
        : base(id => id.Value, value => FromGuid(value))
    {
    }

    private static TId FromGuid(Guid value) => TId.From(value);
}
