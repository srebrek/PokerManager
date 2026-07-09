using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        Assembly moduleAssembly)
    {
        services.AddValidatorsFromAssembly(moduleAssembly, includeInternalTypes: true);

        return services;
    }
}
