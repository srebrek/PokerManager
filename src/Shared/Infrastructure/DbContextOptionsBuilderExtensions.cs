using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder AddDomainEventsClearing(this DbContextOptionsBuilder options) =>
        options.AddInterceptors(new DomainEventsClearingInterceptor());
}
