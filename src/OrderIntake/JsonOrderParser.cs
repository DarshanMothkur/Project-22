using System.Text.Json;

namespace OrderIntake;

internal sealed class MalformedInputException : Exception
{
}

internal static class JsonOrderParser
{
    public static JsonDocument Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new MalformedInputException();
        }

        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new MalformedInputException();
        }
    }
}
