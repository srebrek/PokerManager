using Contracts.Api.Statistics;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Application.Validation;
using Shared.Domain;
using Shared.Presentation;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;

namespace Statistics.Features.GetGameSummary;

internal sealed class GetGameSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            StatisticsRoutes.GameSummary,
            async (
                Guid gameId,
                GetGameSummaryQueryHandler handler,
                IEnumerable<IValidator<GetGameSummaryQuery>> validators,
                CancellationToken ct) =>
            {
                GetGameSummaryQuery query = new(gameId);
                Result<GameSummaryResponse> result =
                    await validators.HandleValidatedAsync(query, handler.Handle, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(StatisticsRoutes.Tag)
            .AllowAnonymous();
    }
}
