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

namespace Gameplay.Features.UndoLastAction;

internal sealed class UndoLastActionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.UndoLastAction,
            async (
                Guid handId,
                UndoLastActionRequest request,
                UndoLastActionCommandHandler handler,
                IEnumerable<IValidator<UndoLastActionCommand>> validators,
                CancellationToken ct) =>
            {
                UndoLastActionCommand command = new(handId, request.ActingParticipantId);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record UndoLastActionCommand(Guid HandId, Guid ActingParticipantId);

internal sealed class UndoLastActionCommandValidator : AbstractValidator<UndoLastActionCommand>
{
    public UndoLastActionCommandValidator()
    {
        RuleFor(c => c.HandId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();
    }
}

internal sealed class UndoLastActionCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(UndoLastActionCommand command, CancellationToken ct)
    {
        Hand? hand = await outbox.DbContext.Hands
            .Include(h => h.Actions.OrderBy(a => a.SequenceNumber))
            .SingleOrDefaultAsync(h => h.Id == HandId.From(command.HandId), ct);

        if (hand is null)
        {
            return HandErrors.HandNotFound;
        }

        Game? game = await outbox.DbContext.Games.SingleOrDefaultAsync(g => g.Id == hand.GameId, ct);
        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        if (game.AuthorizeHandControl(ParticipantId.From(command.ActingParticipantId), hand.Id)
                .TryGetError(out Error? error))
        {
            return error;
        }

        if (hand.UndoLastAction().TryGetError(out error))
        {
            return error;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
