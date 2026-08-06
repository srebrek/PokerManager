namespace Web.Frontend.Common;

internal sealed class ParticipantSession
{
    private ParticipantIdentity? _identity;

    public void Set(Guid gameId, Guid participantId) => _identity = new ParticipantIdentity(gameId, participantId);

    public bool IsMe(Guid gameId, Guid participantId) =>
        _identity is { } identity && identity.GameId == gameId && identity.ParticipantId == participantId;

    private sealed record ParticipantIdentity(Guid GameId, Guid ParticipantId);
}
