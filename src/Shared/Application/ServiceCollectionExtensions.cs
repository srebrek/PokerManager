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
        services.AddCommandAndQueryHandlers(moduleAssembly);

        return services;
    }

    private static IServiceCollection AddCommandAndQueryHandlers(
        this IServiceCollection services,
        Assembly moduleAssembly)
    {
        IEnumerable<Type> handlerTypes = moduleAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsNested: false }
                && (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal)
                    || type.Name.EndsWith("QueryHandler", StringComparison.Ordinal)));

        foreach (Type handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);
        }

        return services;
    }
}
