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

namespace Gameplay.Features.FinishGame;

internal sealed class FinishGameEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.FinishGame,
            async (
                Guid gameId,
                FinishGameRequest request,
                FinishGameCommandHandler handler,
                IEnumerable<IValidator<FinishGameCommand>> validators,
                CancellationToken ct) =>
            {
                FinishGameCommand command = new(gameId, request.ActingParticipantId);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record FinishGameCommand(Guid GameId, Guid ActingParticipantId);

internal sealed class FinishGameCommandValidator : AbstractValidator<FinishGameCommand>
{
    public FinishGameCommandValidator()
    {
        RuleFor(c => c.GameId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();
    }
}

internal sealed class FinishGameCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(FinishGameCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games.SingleOrDefaultAsync(g => g.Id == GameId.From(command.GameId), ct);
        if (game is null)
        {
            return Result.Failure(GameErrors.GameNotFound);
        }

        Result finishResult = game.Finish(ParticipantId.From(command.ActingParticipantId));
        if (finishResult.IsFailure)
        {
            return finishResult;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
