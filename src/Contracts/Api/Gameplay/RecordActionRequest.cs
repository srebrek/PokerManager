using System.Text.Json.Serialization;

namespace Contracts.Api.Gameplay;

public sealed record RecordActionRequest(Guid ActingParticipantId, HandActionType Type, int? AmountTo = null);

[JsonConverter(typeof(JsonStringEnumConverter<HandActionType>))]
public enum HandActionType
{
    Check,
    Call,
    Bet,
    Raise,
    Fold,
    AllIn,
}
