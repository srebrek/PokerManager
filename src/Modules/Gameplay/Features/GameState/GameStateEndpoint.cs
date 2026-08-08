using Contracts.Api.Gameplay;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Application.Validation;
using Shared.Domain;
using Shared.Presentation;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;

namespace Gameplay.Features.GameState;

internal sealed class GameStateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            GameplayRoutes.GameState,
            async (
                Guid gameId,
                GameStateQueryHandler handler,
                IEnumerable<IValidator<GameStateQuery>> validators,
                CancellationToken ct) =>
            {
                GameStateQuery query = new(gameId);
                Result<GameStateResponse> result = await validators.HandleValidatedAsync(query, handler.Handle, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .WithName(GameplayRoutes.GameStateEndpointName)
            .AllowAnonymous();
    }
}
