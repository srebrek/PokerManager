using IntegrationEvents = Contracts.IntegrationEvents.Gameplay;

namespace Statistics.Domain.ValueObjects;

internal enum HandActionType
{
    Check,
    Call,
    Bet,
    Raise,
    Fold,
    AllIn,
    PostSmallBlind,
    PostBigBlind,
}

internal static class HandActionTypeMappings
{
    public static HandActionType ToDomain(this IntegrationEvents.HandActionType type) => type switch
    {
        IntegrationEvents.HandActionType.Check => HandActionType.Check,
        IntegrationEvents.HandActionType.Call => HandActionType.Call,
        IntegrationEvents.HandActionType.Bet => HandActionType.Bet,
        IntegrationEvents.HandActionType.Raise => HandActionType.Raise,
        IntegrationEvents.HandActionType.Fold => HandActionType.Fold,
        IntegrationEvents.HandActionType.AllIn => HandActionType.AllIn,
        IntegrationEvents.HandActionType.PostSmallBlind => HandActionType.PostSmallBlind,
        IntegrationEvents.HandActionType.PostBigBlind => HandActionType.PostBigBlind,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
