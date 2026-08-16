using Contracts.Api.Gameplay;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Shared.Presentation;

namespace Gameplay.Features.SubscribeGameStateChanges;

internal sealed class SubscribeGameStateChangesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapHub<GameHub>(GameplayRoutes.Hub);
}
