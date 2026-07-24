namespace Shared.Domain;

public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors) : Error(
    "Validation.General", "One or more validation errors occurred", ErrorType.Validation);
