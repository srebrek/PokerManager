// Runs the test suite with coverage and turns the raw Cobertura files into a readable report.
//
// Usage: dotnet run scripts/coverage.cs <arguments forwarded to dotnet test e.g. --no-build --configuration Release>

using System.Diagnostics;
using System.Runtime.CompilerServices;

string[] assemblyFilters = ["-*Tests", "-Aspire.*"];
string[] fileFilters =
[
    "-**/obj/**",
    "-**/Migrations/**",
    "-**/*DbContextFactory.cs",
    "-**/Internal/Generated/**",
];

string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ScriptPath())!, ".."));
string results = Path.Combine(root, "TestResults");
string report = Path.Combine(results, "coverage-report");

if (Directory.Exists(results))
{
    Directory.Delete(results, true);
}

int otherTestsExitCode = Run("dotnet",
[
    "test",
    "--filter-not-namespace", "E2ETests",
    "--ignore-exit-code", "8",
    "--coverage",
    "--coverage-output-format", "cobertura",
    "--results-directory", results,
    .. args,
]);

int e2eTestsExitCode = Run("dotnet",
[
    "test",
    "--project", Path.Combine(root, "tests", "E2ETests", "E2ETests.csproj"),
    "--coverage",
    "--coverage-output-format", "cobertura",
    "--results-directory", results,
    .. args,
]);

int testExitCode = otherTestsExitCode is not 0 ? otherTestsExitCode : e2eTestsExitCode;

if (!Directory.Exists(results) || !Directory.EnumerateFiles(results, "*.cobertura.xml").Any())
{
    await Console.Error.WriteLineAsync("No coverage files produced - skipping the report.");
    return testExitCode is 0 ? 1 : testExitCode;
}

int reportExitCode = Run("dotnet",
[
    "tool", "execute", "--yes", "dotnet-reportgenerator-globaltool",
    $"-reports:{Path.Combine(results, "*.cobertura.xml")}",
    $"-targetdir:{report}",
    "-reporttypes:TextSummary;Html;MarkdownSummaryGithub",
    $"-assemblyfilters:{string.Join(';', assemblyFilters)}",
    $"-filefilters:{string.Join(';', fileFilters)}",
    "-verbosity:Warning",
]);

if (reportExitCode is not 0)
{
    return reportExitCode;
}

Console.WriteLine();
Console.WriteLine(await File.ReadAllTextAsync(Path.Combine(report, "Summary.txt")));
Console.WriteLine($"HTML: {Path.Combine(report, "index.html")}");

string? stepSummary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(stepSummary))
{
    await File.AppendAllTextAsync(stepSummary, await File.ReadAllTextAsync(Path.Combine(report, "SummaryGithub.md")));
}

return testExitCode;

static int Run(string file, string[] arguments)
{
    ProcessStartInfo startInfo = new(file);
    foreach (string argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    Console.WriteLine($"> {file} {string.Join(' ', arguments)}");
    using Process process = Process.Start(startInfo)!;
    process.WaitForExit();
    return process.ExitCode;
}

static string ScriptPath([CallerFilePath] string path = "") => path;
