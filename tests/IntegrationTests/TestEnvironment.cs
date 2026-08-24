namespace IntegrationTests;

internal static class TestEnvironment
{
    public static string Name { get; } =
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is { Length: > 0 } name
            ? name
            : "Testing";
}
