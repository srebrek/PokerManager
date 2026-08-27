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

namespace Gameplay.Features.Rebuy;

internal sealed class RebuyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.Rebuy,
            async (
                Guid gameId,
                Guid participantId,
                RebuyRequest request,
                RebuyCommandHandler handler,
                IEnumerable<IValidator<RebuyCommand>> validators,
                CancellationToken ct) =>
            {
                RebuyCommand command = new(gameId, request.ActingParticipantId, participantId, request.Amount);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record RebuyCommand(Guid GameId, Guid ActingParticipantId, Guid TargetParticipantId, int Amount);

internal sealed class RebuyCommandValidator : AbstractValidator<RebuyCommand>
{
    public RebuyCommandValidator()
    {
        RuleFor(c => c.GameId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.TargetParticipantId)
            .NotEmpty();

        RuleFor(c => c.Amount)
            .NotEqual(0);
    }
}

internal sealed class RebuyCommandHandler(
    IDbContextOutbox<GameplayDbContext> outbox,
    TimeProvider timeProvider)
{
    public async Task<Result> Handle(RebuyCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == GameId.From(command.GameId), ct);

        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        Result result = game.Rebuy(
            ParticipantId.From(command.ActingParticipantId),
            ParticipantId.From(command.TargetParticipantId),
            command.Amount);

        if (result.TryGetError(out Error? error))
        {
            return error;
        }

        await outbox.PublishAsync(new RebuyRecordedIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow(),
            game.Id.Value,
            command.TargetParticipantId,
            command.Amount));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
