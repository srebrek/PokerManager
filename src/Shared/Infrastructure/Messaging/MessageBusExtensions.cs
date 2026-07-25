using FluentValidation;
using FluentValidation.Results;
using Shared.Domain;
using Wolverine;

namespace Shared.Infrastructure.Messaging;

public static class MessageBusExtensions
{
    // TODO: remove when analyzer catches up
#pragma warning disable CA1034 // Nested types should not be visible - false positive
    extension(IMessageBus bus)
#pragma warning restore CA1034 // Nested types should not be visible
    {
        public async Task<Result> InvokeValidatedAsync<TCommand>(
            TCommand command,
            IEnumerable<IValidator<TCommand>> validators,
            CancellationToken ct)
            where TCommand : notnull
        {
            ValidationFailure[] failures = await ValidationHelper.ValidateAsync(command, validators);
            if (failures.Length > 0)
            {
                return Result.Failure(ValidationHelper.CreateValidationError(failures));
            }

#pragma warning disable RS0030 // Do not use banned APIs - the single legal InvokeAsync call site
            return await bus.InvokeAsync<Result>(command, ct);
#pragma warning restore RS0030
        }

        public async Task<Result<TResponse>> InvokeValidatedAsync<TCommand, TResponse>(
            TCommand command,
            IEnumerable<IValidator<TCommand>> validators,
            CancellationToken ct)
            where TCommand : notnull
        {
            ValidationFailure[] failures = await ValidationHelper.ValidateAsync(command, validators);
            if (failures.Length > 0)
            {
                return Result.Failure<TResponse>(ValidationHelper.CreateValidationError(failures));
            }

#pragma warning disable RS0030 // Do not use banned APIs - the single legal InvokeAsync call site
            return await bus.InvokeAsync<Result<TResponse>>(command, ct);
#pragma warning restore RS0030
        }
    }
}
