using System.Text.Json;

namespace Web.Frontend.Common.Http;

internal static class ApiErrorReader
{
    public static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            await using Stream stream = await response.Content.ReadAsStreamAsync();
            using JsonDocument document = await JsonDocument.ParseAsync(stream);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Object)
            {
                List<string> messages = [.. errors.EnumerateObject()
                    .Select(property => property.Value)
                    .Where(value => value.ValueKind == JsonValueKind.Array)
                    .SelectMany(value => value.EnumerateArray())
                    .Select(message => message.GetString())
                    .OfType<string>()];

                if (messages.Count > 0)
                {
                    return string.Join(" ", messages);
                }
            }

            if (root.TryGetProperty("detail", out JsonElement detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString();
            }

            if (root.TryGetProperty("title", out JsonElement title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString();
            }
        }
        catch (JsonException)
        {
            // Non-JSON body — fall through to the caller's generic message.
        }

        return null;
    }
}
