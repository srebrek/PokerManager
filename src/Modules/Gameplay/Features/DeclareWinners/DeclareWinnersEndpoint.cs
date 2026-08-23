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

namespace Gameplay.Features.DeclareWinners;

internal sealed class DeclareWinnersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.HandWinners,
            async (
                Guid handId,
                DeclareWinnersRequest request,
                DeclareWinnersCommandHandler handler,
                IEnumerable<IValidator<DeclareWinnersCommand>> validators,
                CancellationToken ct) =>
            {
                DeclareWinnersCommand command = new(handId, request.ActingParticipantId, request.Winners);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record DeclareWinnersCommand(
    Guid HandId, Guid ActingParticipantId, IReadOnlyList<PotWinner> Winners);

internal sealed class DeclareWinnersCommandValidator : AbstractValidator<DeclareWinnersCommand>
{
    public DeclareWinnersCommandValidator()
    {
        RuleFor(c => c.HandId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.Winners)
            .NotEmpty();

        RuleForEach(c => c.Winners)
            .Must(winner => winner.PotIndex >= 0 && winner.ParticipantId != Guid.Empty);
    }
}

internal sealed class DeclareWinnersCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(DeclareWinnersCommand command, CancellationToken ct)
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

        Game? game = await outbox.DbContext.Games
            .SingleOrDefaultAsync(g => g.Id == hand.GameId, ct);
        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        if (game.HostParticipantId != ParticipantId.From(command.ActingParticipantId))
        {
            return GameErrors.NotHost;
        }

        List<HandPotWinner> potWinners = [.. command.Winners.Select(winner => new HandPotWinner(
            winner.PotIndex,
            ParticipantId.From(winner.ParticipantId)))];

        if (hand.DeclareWinners(potWinners).TryGetError(out Error? error))
        {
            return error;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return Result.Success();
    }
}
