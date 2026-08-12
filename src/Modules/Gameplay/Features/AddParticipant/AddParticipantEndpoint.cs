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

namespace Gameplay.Features.AddParticipant;

internal sealed class AddParticipantEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.AddParticipant,
            async (
                Guid gameId,
                AddParticipantRequest request,
                AddParticipantCommandHandler handler,
                IEnumerable<IValidator<AddParticipantCommand>> validators,
                CancellationToken ct) =>
            {
                AddParticipantCommand command = new(gameId, request.Name);
                Result<AddParticipantResponse> result =
                    await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}
