using Api = Contracts.Api.Gameplay;

namespace Gameplay.Domain.ValueObjects;

internal enum Street
{
    PreFlop,
    Flop,
    Turn,
    River,
    Finished,
}

internal static class StreetMappings
{
    public static Api.Street ToContract(this Street street) => street switch
    {
        Street.PreFlop => Api.Street.PreFlop,
        Street.Flop => Api.Street.Flop,
        Street.Turn => Api.Street.Turn,
        Street.River => Api.Street.River,
        Street.Finished => Api.Street.Finished,
        _ => throw new ArgumentOutOfRangeException(nameof(street), street, null),
    };
}
