namespace Shared.Infrastructure;

public static class DatabaseConstants
{
    /// <summary>
    /// Connection string name injected by Aspire. Must match the database resource name
    /// in Aspire.AppHost/AppHost.cs.
    /// </summary>
    public const string ConnectionStringName = "PokerManager-db";
}
