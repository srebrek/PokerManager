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

namespace Gameplay.Features.GetHandState;

internal sealed class GetHandStateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            GameplayRoutes.Hand,
            async (
                Guid handId,
                GetHandStateQueryHandler handler,
                IEnumerable<IValidator<GetHandStateQuery>> validators,
                CancellationToken ct) =>
            {
                GetHandStateQuery query = new(handId);
                Result<GetHandStateResponse> result = await validators.HandleValidatedAsync(query, handler.Handle, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .WithName(GameplayRoutes.HandRouteName)
            .AllowAnonymous();
    }
}
