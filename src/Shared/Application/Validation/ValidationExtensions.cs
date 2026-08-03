using FluentValidation;
using FluentValidation.Results;
using Shared.Domain;

namespace Shared.Application.Validation;

public static class ValidationExtensions
{
    public static async Task<Result> HandleValidatedAsync<TCommand>(
        this IEnumerable<IValidator<TCommand>> validators,
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> handle,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(handle);

        ValidationFailure[] failures = await ValidateAsync(command, validators);

        return failures.Length > 0
            ? Result.Failure(CreateValidationError(failures))
            : await handle(command, ct);
    }

    public static async Task<Result<TResponse>> HandleValidatedAsync<TCommand, TResponse>(
        this IEnumerable<IValidator<TCommand>> validators,
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TResponse>>> handle,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(handle);

        ValidationFailure[] failures = await ValidateAsync(command, validators);

        return failures.Length > 0
            ? Result.Failure<TResponse>(CreateValidationError(failures))
            : await handle(command, ct);
    }

    private static async Task<ValidationFailure[]> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators)
    {
        if (!validators.Any())
        {
            return [];
        }

        ValidationContext<TCommand> context = new(command);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context)));

        return [.. validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)];
    }

    private static ValidationError CreateValidationError(ValidationFailure[] failures) =>
        new(failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray()));
}
