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

namespace Gameplay.Features.ChangeGameRules;

internal sealed class ChangeGameRulesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.Rules,
            async (
                Guid gameId,
                ChangeGameRulesRequest request,
                ChangeGameRulesCommandHandler handler,
                IEnumerable<IValidator<ChangeGameRulesCommand>> validators,
                CancellationToken ct) =>
            {
                ChangeGameRulesCommand command = new(
                    gameId,
                    request.ActingParticipantId,
                    request.SmallBlind,
                    request.BigBlind);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record ChangeGameRulesCommand(
    Guid GameId,
    Guid ActingParticipantId,
    int SmallBlind,
    int BigBlind);

internal sealed class ChangeGameRulesCommandValidator : AbstractValidator<ChangeGameRulesCommand>
{
    public ChangeGameRulesCommandValidator()
    {
        RuleFor(c => c.GameId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.SmallBlind)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.BigBlind)
            .GreaterThanOrEqualTo(0)
            .GreaterThanOrEqualTo(c => c.SmallBlind);
    }
}

internal sealed class ChangeGameRulesCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(ChangeGameRulesCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games
            .SingleOrDefaultAsync(g => g.Id == GameId.From(command.GameId), ct);

        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        Result result = game.ChangeRules(
            ParticipantId.From(command.ActingParticipantId),
            command.SmallBlind,
            command.BigBlind);

        if (result.TryGetError(out Error? error))
        {
            return error;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return Result.Success();
    }
}
