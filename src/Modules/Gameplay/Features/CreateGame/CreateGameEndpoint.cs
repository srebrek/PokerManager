using Contracts.Gameplay;
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

namespace Gameplay.Features.CreateGame;

internal sealed class CreateGameEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.CreateGame,
            async (
                CreateGameRequest request,
                IMessageBus bus,
                IEnumerable<IValidator<CreateGameCommand>> validators,
                CancellationToken ct) =>
            {
                CreateGameCommand command = new(request.HostName);
                Result<CreateGameResponse> result =
                    await bus.InvokeValidatedAsync<CreateGameCommand, CreateGameResponse>(command, validators, ct);
                return result.Match(
                    value => Results.Created($"gameplay/games/{value.GameId}", value), // TODO: use LinkGenerator
                    CustomResults.Problem);
            })
            .WithTags("Gameplay")
            .AllowAnonymous();
    }
}
