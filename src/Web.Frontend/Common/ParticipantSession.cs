namespace Web.Frontend.Common;

internal sealed class ParticipantSession
{
    private ParticipantIdentity? _identity;

    public void Set(Guid gameId, Guid participantId) => _identity = new ParticipantIdentity(gameId, participantId);

    public Guid? ParticipantIdFor(Guid gameId) =>
        _identity is not null && _identity.GameId == gameId
        ? _identity.ParticipantId
        : null;

    private sealed record ParticipantIdentity(Guid GameId, Guid ParticipantId);
}
