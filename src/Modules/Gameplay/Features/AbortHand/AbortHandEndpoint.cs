using Contracts.Api.Gameplay;
using Contracts.IntegrationEvents.Gameplay;
using FluentValidation;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Validation;
using Shared.Domain;
using Shared.Presentation;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;
using Wolverine.EntityFrameworkCore;

namespace Gameplay.Features.AbortHand;

internal sealed class AbortHandEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.AbortHand,
            async (
                Guid handId,
                AbortHandRequest request,
                AbortHandCommandHandler handler,
                IEnumerable<IValidator<AbortHandCommand>> validators,
                CancellationToken ct) =>
            {
                AbortHandCommand command = new(handId, request.ActingParticipantId);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record AbortHandCommand(Guid HandId, Guid ActingParticipantId);

internal sealed class AbortHandCommandValidator : AbstractValidator<AbortHandCommand>
{
    public AbortHandCommandValidator()
    {
        RuleFor(c => c.HandId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();
    }
}

internal sealed class AbortHandCommandHandler(
    IDbContextOutbox<GameplayDbContext> outbox,
    TimeProvider timeProvider)
{
    // TODO: add tests
    public async Task<Result> Handle(AbortHandCommand command, CancellationToken ct)
    {
        Hand? hand = await outbox.DbContext.Hands.SingleOrDefaultAsync(h => h.Id == HandId.From(command.HandId), ct);
        if (hand is null)
        {
            return HandErrors.HandNotFound;
        }

        if (hand.Abort().TryGetError(out Error? error))
        {
            return error;
        }

        Game? game = await outbox.DbContext.Games.SingleOrDefaultAsync(g => g.Id == hand.GameId, ct);
        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        if (game.DetachHand(ParticipantId.From(command.ActingParticipantId), hand.Id).TryGetError(out error))
        {
            return error;
        }

        await outbox.PublishAsync(new HandAbortedIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow(),
            hand.Id.Value));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
