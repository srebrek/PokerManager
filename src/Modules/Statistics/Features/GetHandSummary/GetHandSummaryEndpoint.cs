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

namespace Statistics.Features.GetHandSummary;

internal sealed class GetHandSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            StatisticsRoutes.HandSummary,
            async (
                Guid handId,
                GetHandSummaryQueryHandler handler,
                IEnumerable<IValidator<GetHandSummaryQuery>> validators,
                CancellationToken ct) =>
            {
                GetHandSummaryQuery query = new(handId);
                Result<HandSummaryResponse> result =
                    await validators.HandleValidatedAsync(query, handler.Handle, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(StatisticsRoutes.Tag)
            .AllowAnonymous();
    }
}
