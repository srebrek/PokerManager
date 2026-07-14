using System.Globalization;
using System.Security.Cryptography;

namespace Gameplay.Domain.ValueObjects;

internal readonly record struct JoinCode
{
    public string Value { get; }

    private JoinCode(string value) => Value = value;

    public static JoinCode Generate() =>
        new(RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture));

    public static JoinCode From(string value) => new(value);
}
