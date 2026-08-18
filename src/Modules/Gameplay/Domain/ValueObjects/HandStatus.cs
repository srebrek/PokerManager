using Api = Contracts.Api.Gameplay;

namespace Gameplay.Domain.ValueObjects;

internal enum HandStatus
{
    InProgress,
    Finished,
    Aborted,
}

internal static class HandStatusMappings
{
    public static Api.HandStatus ToContract(this HandStatus status) => status switch
    {
        HandStatus.InProgress => Api.HandStatus.InProgress,
        HandStatus.Finished => Api.HandStatus.Finished,
        HandStatus.Aborted => Api.HandStatus.Aborted,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
