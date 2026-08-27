using Contracts.Api.Gameplay;
using Contracts.IntegrationEvents.Gameplay;
using FluentValidation;
using Gameplay.Domain.Entities;
using Gameplay.Domain.Services;
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

namespace Gameplay.Features.FinishHand;

internal sealed class FinishHandEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.FinishHand,
            async (
                Guid handId,
                FinishHandRequest request,
                FinishHandCommandHandler handler,
                IEnumerable<IValidator<FinishHandCommand>> validators,
                CancellationToken ct) =>
            {
                FinishHandCommand command = new(handId, request.ActingParticipantId);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record FinishHandCommand(Guid HandId, Guid ActingParticipantId);

internal sealed class FinishHandCommandValidator : AbstractValidator<FinishHandCommand>
{
    public FinishHandCommandValidator()
    {
        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.HandId)
            .NotEmpty();
    }
}

internal sealed class FinishHandCommandHandler(
    IDbContextOutbox<GameplayDbContext> outbox,
    TimeProvider timeProvider)
{
    public async Task<Result> Handle(FinishHandCommand command, CancellationToken ct)
    {
        Hand? hand = await outbox.DbContext.Hands
            .Include(h => h.Seats)
            .Include(h => h.Actions.OrderBy(a => a.SequenceNumber))
            .Include(h => h.PotWinners)
            .SingleOrDefaultAsync(h => h.Id == HandId.From(command.HandId), ct);

        if (hand is null)
        {
            return HandErrors.HandNotFound;
        }

        if (!hand.Finish().TryGetValue(out List<HandAward>? awards, out Error? error))
        {
            return error;
        }

        Game? game = await outbox.DbContext.Games
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == hand.GameId, ct);

        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        if (game.ApplyHandAwards(awards, ParticipantId.From(command.ActingParticipantId), hand.Id)
                .TryGetError(out error))
        {
            return error;
        }

        if (!HandStateCalculator.Calculate(hand.Seats, hand.Actions)
                .TryGetValue(out HandState handState, out error))
        {
            return error;
        }

        Dictionary<ParticipantId, int> netByParticipantId = awards.ToDictionary(a => a.ParticipantId, a => a.Net);

        await outbox.PublishAsync(new HandFinishedIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow(),
            hand.Id.Value,
            [.. handState.Pots.Select(pot => new HandFinishedPot(pot.Index, pot.Amount.Value))],
            [.. hand.PotWinners.Select(winner => new HandFinishedPotWinner(
                winner.PotIndex,
                winner.ParticipantId.Value))],
            [.. game.Participants.Select(participant => new HandFinishedResult(
                participant.Id.Value,
                participant.Name,
                netByParticipantId.GetValueOrDefault(participant.Id),
                participant.Chips.Value))]));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
