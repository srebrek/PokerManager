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

namespace Gameplay.Features.MoveDealerButton;

internal sealed class MoveDealerButtonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.DealerButton,
            async (
                Guid gameId,
                MoveDealerButtonRequest request,
                MoveDealerButtonCommandHandler handler,
                IEnumerable<IValidator<MoveDealerButtonCommand>> validators,
                CancellationToken ct) =>
            {
                MoveDealerButtonCommand command = new(
                    gameId,
                    request.ActingParticipantId,
                    request.DealerParticipantId);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record MoveDealerButtonCommand(Guid GameId, Guid ActingParticipantId, Guid DealerParticipantId);

internal sealed class MoveDealerButtonCommandValidator : AbstractValidator<MoveDealerButtonCommand>
{
    public MoveDealerButtonCommandValidator()
    {
        RuleFor(c => c.GameId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.DealerParticipantId)
            .NotEmpty();
    }
}

internal sealed class MoveDealerButtonCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(MoveDealerButtonCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == GameId.From(command.GameId), ct);

        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        Result result = game.MoveDealerButton(
            ParticipantId.From(command.ActingParticipantId),
            ParticipantId.From(command.DealerParticipantId));

        if (result.TryGetError(out Error? error))
        {
            return error;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
