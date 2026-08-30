namespace Statistics;

public sealed class IngestParentMissingException : Exception
{
    public IngestParentMissingException()
    {
    }

    public IngestParentMissingException(string message) : base(message)
    {
    }

    public IngestParentMissingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public static IngestParentMissingException ForHand(Guid handId) =>
        new($"Hand '{handId}' has not been ingested yet.");

    public static IngestParentMissingException ForAction(Guid handId, int sequenceNumber) =>
        new($"Action {sequenceNumber} of hand '{handId}' has not been ingested yet.");
}
