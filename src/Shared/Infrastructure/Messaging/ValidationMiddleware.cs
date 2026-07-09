using FluentValidation;
using FluentValidation.Results;
using Shared.Domain;
using Wolverine;

namespace Shared.Infrastructure.Messaging;

public static class ValidationMiddleware
{
    public static async Task<(HandlerContinuation, Result?)> BeforeAsync<T>(
        T message,
        IEnumerable<IValidator<T>> validators,
        CancellationToken ct)
    {
        ValidationFailure[] failures = await ValidationHelper.ValidateAsync(message, validators);
        if (failures.Length == 0)
        {
            return (HandlerContinuation.Continue, null);
        }

        return (HandlerContinuation.Stop, Result.Failure(ValidationHelper.CreateValidationError(failures)));
    }
}
