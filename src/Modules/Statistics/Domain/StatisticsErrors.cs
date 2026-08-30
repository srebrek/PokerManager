using Shared.Domain;

namespace Statistics.Domain;

internal static class StatisticsErrors
{
    public static readonly Error HandSummaryNotAvailable = Error.NotFound(
        "Statistics.HandSummary.NotAvailable",
        "No finished hand with the given id has been ingested.");
}
