// Builds the solution the way production is built, so the test suite exercises the production paths.
//
// Usage: dotnet run scripts/build.cs <arguments forwarded to dotnet build e.g. --no-restore --configuration Release>

using System.Diagnostics;
using System.Runtime.CompilerServices;

const string DummyConnectionString = "Host=127.0.0.1;Port=1;Database=codegen;Username=codegen";

string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ScriptPath())!, ".."));
string bootstrapper = Path.Combine(root, "src", "Api.Bootstrapper");
string generated = Path.Combine(bootstrapper, "Internal", "Generated");
string configuration = ReadConfiguration(args);

int exitCode = Run("dotnet", ["build", bootstrapper, .. args]);
if (exitCode is not 0)
{
    return exitCode;
}

if (Directory.Exists(generated))
{
    Directory.Delete(generated, true);
}

exitCode = RunHost(configuration, "codegen", "write");
if (exitCode is not 0)
{
    return exitCode;
}

exitCode = Run("dotnet", ["build", .. args]);
if (exitCode is not 0)
{
    return exitCode;
}

return RunHost(configuration, "codegen", "test");

int RunHost(string config, params string[] commandArguments)
{
    return Run("dotnet",
    [
        "run",
        "--project", bootstrapper,
        "--no-launch-profile",
        "--no-build",
        "--configuration", config,
        "--",
        .. commandArguments,
    ],
    new Dictionary<string, string>
    {
        ["ASPNETCORE_ENVIRONMENT"] = "Production",
        ["ConnectionStrings__PokerManager-db"] = DummyConnectionString,
    });
}

static string ReadConfiguration(string[] arguments)
{
    for (int i = 0; i < arguments.Length - 1; i++)
    {
        if (arguments[i] is "--configuration" or "-c")
        {
            return arguments[i + 1];
        }
    }

    return "Debug";
}

static int Run(string file, string[] arguments, Dictionary<string, string>? environment = null)
{
    ProcessStartInfo startInfo = new(file);
    foreach (string argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    foreach ((string key, string value) in environment ?? [])
    {
        startInfo.Environment[key] = value;
    }

    Console.WriteLine($"> {file} {string.Join(' ', arguments)}");
    using Process process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Could not start '{file}'.");
    process.WaitForExit();
    return process.ExitCode;
}

static string ScriptPath([CallerFilePath] string path = "") => path;
