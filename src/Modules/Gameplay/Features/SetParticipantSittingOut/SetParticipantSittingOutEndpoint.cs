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

namespace Gameplay.Features.SetParticipantSittingOut;

internal sealed class SetParticipantSittingOutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.SetParticipantSittingOut,
            async (
                Guid gameId,
                Guid participantId,
                SetParticipantSittingOutRequest request,
                SetParticipantSittingOutCommandHandler handler,
                IEnumerable<IValidator<SetParticipantSittingOutCommand>> validators,
                CancellationToken ct) =>
            {
                SetParticipantSittingOutCommand command = new(
                    gameId,
                    request.ActingParticipantId,
                    participantId,
                    request.IsSittingOut);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record SetParticipantSittingOutCommand(
    Guid GameId,
    Guid ActingParticipantId,
    Guid TargetParticipantId,
    bool IsSittingOut);

internal sealed class SetParticipantSittingOutCommandValidator : AbstractValidator<SetParticipantSittingOutCommand>
{
    public SetParticipantSittingOutCommandValidator()
    {
        RuleFor(c => c.GameId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.TargetParticipantId)
            .NotEmpty();
    }
}

internal sealed class SetParticipantSittingOutCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(SetParticipantSittingOutCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == GameId.From(command.GameId), ct);

        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        Result result = game.SetParticipantSittingOut(
            ParticipantId.From(command.ActingParticipantId),
            ParticipantId.From(command.TargetParticipantId),
            command.IsSittingOut);

        if (result.TryGetError(out Error? error))
        {
            return error;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
