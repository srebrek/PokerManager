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

namespace Gameplay.Features.GetGameByJoinCode;

internal sealed class GetGameByJoinCodeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            GameplayRoutes.GetGameByJoinCode,
            async (
                string joinCode,
                GetGameByJoinCodeQueryHandler handler,
                IEnumerable<IValidator<GetGameByJoinCodeQuery>> validators,
                CancellationToken ct) =>
            {
                GetGameByJoinCodeQuery query = new(joinCode);
                Result<GameLookupResponse> result =
                    await validators.HandleValidatedAsync(query, handler.Handle, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}
