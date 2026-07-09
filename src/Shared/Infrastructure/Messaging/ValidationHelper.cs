using FluentValidation;
using FluentValidation.Results;
using Shared.Domain;

namespace Shared.Infrastructure.Messaging;

internal static class ValidationHelper
{
    public static async Task<ValidationFailure[]> ValidateAsync<TMessage>(
        TMessage message,
        IEnumerable<IValidator<TMessage>> validators)
    {
        if (!validators.Any())
        {
            return [];
        }

        ValidationContext<TMessage> context = new(message);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context)));

        return [.. validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)];
    }

    public static ValidationError CreateValidationError(ValidationFailure[] failures) =>
        new([.. failures.Select(f => Error.Problem(f.ErrorCode, f.ErrorMessage))]);
}
