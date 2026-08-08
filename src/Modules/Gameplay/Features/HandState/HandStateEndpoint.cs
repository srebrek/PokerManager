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

namespace Gameplay.Features.HandState;

internal sealed class HandStateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            GameplayRoutes.HandState,
            async (
                Guid handId,
                HandStateQueryHandler handler,
                IEnumerable<IValidator<HandStateQuery>> validators,
                CancellationToken ct) =>
            {
                HandStateQuery query = new(handId);
                Result<HandStateResponse> result = await validators.HandleValidatedAsync(query, handler.Handle, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .WithName(GameplayRoutes.HandStateEndpointName)
            .AllowAnonymous();
    }
}
