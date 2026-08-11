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

namespace Gameplay.Features.RecordAction;

internal sealed class RecordActionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.RecordAction,
            async (
                Guid handId,
                RecordActionRequest request,
                RecordActionCommandHandler handler,
                IEnumerable<IValidator<RecordActionCommand>> validators,
                CancellationToken ct) =>
            {
                RecordActionCommand command = new(handId, request.ActingParticipantId, request.Type, request.AmountTo);
                Result result = await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(GameplayRoutes.Tag)
            .AllowAnonymous();
    }
}

internal sealed record RecordActionCommand(Guid HandId, Guid ActingParticipantId, Contracts.Api.Gameplay.HandActionType Type, int? AmountTo = null);

internal sealed class RecordActionCommandValidator : AbstractValidator<RecordActionCommand>
{
    public RecordActionCommandValidator()
    {
        RuleFor(c => c.HandId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();

        RuleFor(c => c.Type)
            .NotNull()
            .IsInEnum();

        When(
            c => c.Type is Contracts.Api.Gameplay.HandActionType.Bet or Contracts.Api.Gameplay.HandActionType.Raise,
            () =>
            {
                RuleFor(c => c.AmountTo)
                    .NotNull()
                    .GreaterThan(0);
            })
            .Otherwise(() =>
            {
                RuleFor(c => c.AmountTo)
                    .Null();
            });
    }
}

internal sealed class RecordActionCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result> Handle(RecordActionCommand command, CancellationToken ct)
    {
        Hand? hand = await outbox.DbContext.Hands
            .Include(h => h.Seats)
            .Include(h => h.Actions.OrderBy(a => a.SequenceNumber))
            .SingleOrDefaultAsync(h => h.Id == HandId.From(command.HandId), ct);

        if (hand is null)
        {
            return Result.Failure(HandErrors.HandNotFound);
        }

        // TODO: consider better enum mapping
        Result recordActionResult = hand.RecordAction(
            ParticipantId.From(command.ActingParticipantId),
            (Domain.ValueObjects.HandActionType)command.Type,
            command.AmountTo is not null ? ChipsStack.Create(command.AmountTo.Value).Value : null);

        if (recordActionResult.IsFailure)
        {
            return Result.Failure(recordActionResult.Error);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return recordActionResult;
    }
}
