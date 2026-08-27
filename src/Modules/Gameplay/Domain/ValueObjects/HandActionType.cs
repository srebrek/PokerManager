using Api = Contracts.Api.Gameplay;
using IntegrationEvents = Contracts.IntegrationEvents.Gameplay;

namespace Gameplay.Domain.ValueObjects;

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
    public static HandActionType ToDomain(this Api.HandActionType type) => type switch
    {
        Api.HandActionType.Check => HandActionType.Check,
        Api.HandActionType.Call => HandActionType.Call,
        Api.HandActionType.Bet => HandActionType.Bet,
        Api.HandActionType.Raise => HandActionType.Raise,
        Api.HandActionType.Fold => HandActionType.Fold,
        Api.HandActionType.AllIn => HandActionType.AllIn,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static IntegrationEvents.HandActionType ToContract(this HandActionType type) => type switch
    {
        HandActionType.Check => IntegrationEvents.HandActionType.Check,
        HandActionType.Call => IntegrationEvents.HandActionType.Call,
        HandActionType.Bet => IntegrationEvents.HandActionType.Bet,
        HandActionType.Raise => IntegrationEvents.HandActionType.Raise,
        HandActionType.Fold => IntegrationEvents.HandActionType.Fold,
        HandActionType.AllIn => IntegrationEvents.HandActionType.AllIn,
        HandActionType.PostSmallBlind => IntegrationEvents.HandActionType.PostSmallBlind,
        HandActionType.PostBigBlind => IntegrationEvents.HandActionType.PostBigBlind,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
