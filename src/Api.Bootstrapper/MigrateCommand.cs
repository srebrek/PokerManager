using System.Diagnostics.CodeAnalysis;
using Gameplay;
using Identity;
using JasperFx.CommandLine;
using JasperFx.Resources;
using Statistics;

namespace Api.Bootstrapper;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Wolverine")]
public sealed class MigrateCommand : JasperFxAsyncCommand<NetCoreInput>
{
    public override async Task<bool> Execute(NetCoreInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        using IHost host = input.BuildHost();

        await host.Services.ApplyIdentityMigrationsAsync();
        await host.Services.ApplyGameplayMigrationsAsync();
        await host.Services.ApplyStatisticsMigrationsAsync();

        await host.SetupResources(CancellationToken.None);

        return true;
    }
}
