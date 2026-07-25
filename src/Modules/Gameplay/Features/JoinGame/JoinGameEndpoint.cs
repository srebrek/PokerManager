using Contracts.Api.Gameplay;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Domain;
using Shared.Infrastructure.Messaging;
using Shared.Presentation;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;
using Wolverine;

namespace Gameplay.Features.JoinGame;

internal sealed class JoinGameEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.JoinGame,
            async (
                JoinGameRequest request,
                IMessageBus bus,
                IEnumerable<IValidator<JoinGameCommand>> validators,
                CancellationToken ct) =>
            {
                JoinGameCommand command = new(request.ParticipantName, request.JoinCode);
                Result<JoinGameResponse> result =
                    await bus.InvokeValidatedAsync<JoinGameCommand, JoinGameResponse>(command, validators, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags("Gameplay")
            .AllowAnonymous();
    }
}
