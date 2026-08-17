using System.Text.Json;

namespace Web.Frontend.Common.Http;

internal static class ApiErrorReader
{
    public static async Task<ApiErrorBody> ReadAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        try
        {
            await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            JsonElement root = document.RootElement;

            return new ApiErrorBody(ReadCode(root), ReadMessage(root));
        }
        catch (JsonException)
        {
            // Non-JSON body — fall through to the caller's generic message.
            return default;
        }
    }

    private static string? ReadCode(JsonElement root) =>
        root.TryGetProperty("errorCode", out JsonElement code) && code.ValueKind is JsonValueKind.String
            ? code.GetString()
            : null;

    private static string? ReadMessage(JsonElement root)
    {
        if (root.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Object)
        {
            List<string> messages = [.. errors.EnumerateObject()
                .Select(property => property.Value)
                .Where(value => value.ValueKind is JsonValueKind.Array)
                .SelectMany(value => value.EnumerateArray())
                .Select(message => message.GetString())
                .OfType<string>()];

            if (messages.Count > 0)
            {
                return string.Join(" ", messages);
            }
        }

        if (root.TryGetProperty("detail", out JsonElement detail) && detail.ValueKind is JsonValueKind.String)
        {
            return detail.GetString();
        }

        if (root.TryGetProperty("title", out JsonElement title) && title.ValueKind is JsonValueKind.String)
        {
            return title.GetString();
        }

        return null;
    }
}

internal readonly record struct ApiErrorBody(string? Code, string? Message);
