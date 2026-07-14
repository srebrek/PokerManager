using Shared.Domain;

namespace Gameplay.Domain.ValueObjects;

internal readonly record struct ChipsStack
{
    public int Value { get; }

    private ChipsStack(int value) => Value = value;

    public static Result<ChipsStack> Create(int value)
    {
        if (value < 0)
        {
            return Result.Failure<ChipsStack>(NegativeValueError);
        }

        return new ChipsStack(value);
    }

    public static readonly Error NegativeValueError =
        Error.Problem("Gameplay.ChipsStack.NegativeValue", "Chips stack value cannot be negative.");

    public ChipsStack Add(int amount) => new(Value + amount);

    public Result<ChipsStack> Subtract(int amount) => Create(Value - amount);
}
