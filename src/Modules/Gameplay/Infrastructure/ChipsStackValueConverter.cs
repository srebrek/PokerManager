using Gameplay.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gameplay.Infrastructure;

internal sealed class ChipsStackValueConverter : ValueConverter<ChipsStack, int>
{
    public ChipsStackValueConverter() : base(chips => chips.Value, value => ChipsStack.Create(value).Value)
    {
    }
}
