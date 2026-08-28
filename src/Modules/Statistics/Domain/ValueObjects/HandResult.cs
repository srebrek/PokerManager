namespace Statistics.Domain.ValueObjects;

internal sealed record HandResult(Guid ParticipantId, string ParticipantName, int Net, int EndingChips);
