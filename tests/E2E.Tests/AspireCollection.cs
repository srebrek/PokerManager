namespace E2E.Tests;

[CollectionDefinition("Aspire Collection", DisableParallelization = true)]
#pragma warning disable CA1515 // xUnit requires collection fixtures to be public.
public sealed class AspireCollection : ICollectionFixture<AspireFixture>;
#pragma warning restore CA1515
