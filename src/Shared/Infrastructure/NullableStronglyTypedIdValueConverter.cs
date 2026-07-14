using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shared.Domain;

namespace Shared.Infrastructure;

// TODO: To delete
public sealed class NullableStronglyTypedIdValueConverter<TId> : ValueConverter<TId?, Guid?>
    where TId : struct, IStronglyTypedId<TId>
{
    public NullableStronglyTypedIdValueConverter()
        : base(id => id.HasValue ? id.Value.Value : null, value => FromGuid(value))
    {
    }

    private static TId? FromGuid(Guid? value) => value.HasValue ? TId.From(value.Value) : null;
}
