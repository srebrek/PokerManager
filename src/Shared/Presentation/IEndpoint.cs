using Microsoft.AspNetCore.Routing;

namespace Shared.Presentation;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
