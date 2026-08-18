using Contracts.Api.Gameplay;
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
                FinishHandCommand command = new(handId, request.ActingParticipantId, request.WinnerParticipantId);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record FinishHandCommand(Guid HandId, Guid ActingParticipantId, Guid WinnerParticipantId);

internal sealed class FinishHandCommandValidator : AbstractValidator<FinishHandCommand>
{
    public FinishHandCommandValidator()
    {
        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.HandId)
            .NotEmpty();

        RuleFor(c => c.WinnerParticipantId)
            .NotEmpty();
    }
}

internal sealed class FinishHandCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(FinishHandCommand command, CancellationToken ct)
    {
        Hand? hand = await outbox.DbContext.Hands
            .Include(h => h.Seats)
            .Include(h => h.Actions.OrderBy(a => a.SequenceNumber))
            .SingleOrDefaultAsync(h => h.Id == HandId.From(command.HandId), ct);

        if (hand is null)
        {
            return HandErrors.HandNotFound;
        }

        if (!hand.Finish(ParticipantId.From(command.WinnerParticipantId))
                .TryGetValue(out List<HandAward>? awards, out Error? error))
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

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
