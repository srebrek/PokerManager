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

namespace Gameplay.Features.MoveParticipantDown;

internal sealed class MoveParticipantDownEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.MoveParticipantDown,
            async (
                Guid gameId,
                Guid participantId,
                MoveParticipantDownRequest request,
                MoveParticipantDownCommandHandler handler,
                IEnumerable<IValidator<MoveParticipantDownCommand>> validators,
                CancellationToken ct) =>
            {
                MoveParticipantDownCommand command = new(gameId, request.ActingParticipantId, participantId);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record MoveParticipantDownCommand(Guid GameId, Guid ActingParticipantId, Guid TargetParticipantId);

internal sealed class MoveParticipantDownCommandValidator : AbstractValidator<MoveParticipantDownCommand>
{
    public MoveParticipantDownCommandValidator()
    {
        RuleFor(c => c.GameId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.TargetParticipantId)
            .NotEmpty();
    }
}

internal sealed class MoveParticipantDownCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(MoveParticipantDownCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games
            .SingleOrDefaultAsync(g => g.Id == GameId.From(command.GameId), ct);

        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        Result result = game.MoveParticipantDown(
            ParticipantId.From(command.ActingParticipantId),
            ParticipantId.From(command.TargetParticipantId));

        if (result.TryGetError(out Error? error))
        {
            return error;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
