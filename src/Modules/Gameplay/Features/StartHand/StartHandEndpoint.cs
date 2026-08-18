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

namespace Gameplay.Features.StartHand;

internal sealed class StartHandEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.GameHands,
            async (
                Guid gameId,
                StartHandRequest request,
                StartHandCommandHandler handler,
                IEnumerable<IValidator<StartHandCommand>> validators,
                CancellationToken ct) =>
            {
                StartHandCommand command = new(gameId, request.ActingParticipantId);
                Result<StartHandResponse> result =
                    await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(
                    value => Results.CreatedAtRoute(
                        GameplayRoutes.HandRouteName,
                        new { handId = value.HandId },
                        value),
                    CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}
