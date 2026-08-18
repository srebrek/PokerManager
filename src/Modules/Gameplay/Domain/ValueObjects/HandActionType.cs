using Api = Contracts.Api.Gameplay;

namespace Gameplay.Domain.ValueObjects;

internal enum HandActionType
{
    Check,
    Call,
    Bet,
    Raise,
    Fold,
    PostSmallBlind,
    PostBigBlind,
}

internal static class HandActionTypeMappings
{
    public static HandActionType ToDomain(this Api.HandActionType type) => type switch
    {
        Api.HandActionType.Check => HandActionType.Check,
        Api.HandActionType.Call => HandActionType.Call,
        Api.HandActionType.Bet => HandActionType.Bet,
        Api.HandActionType.Raise => HandActionType.Raise,
        Api.HandActionType.Fold => HandActionType.Fold,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
