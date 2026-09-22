using System.Globalization;
using System.Text.Json;

namespace OrderIntake;

internal static class OrderBuilder
{
    public static Order Build(Dictionary<string, JsonElement> fields)
    {
        return new Order
        {
            OrderId = GetRequiredString(fields, "orderId"),
            PatientId = GetRequiredString(fields, "patientId"),
            SpecimenId = GetRequiredString(fields, "specimenId"),
            SpecimenType = NormalizeSpecimenType(GetRequiredString(fields, "specimenType")),
            Priority = NormalizePriority(GetRequiredString(fields, "priority")),
            CollectionDate = DateOnly.ParseExact(GetRequiredString(fields, "collectionDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            RequestedTests = GetRequestedTests(fields)
        };
    }

    private static string GetRequiredString(Dictionary<string, JsonElement> fields, string fieldName)
    {
        if (!fields.TryGetValue(fieldName, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Missing or null field: {fieldName}");
        }

        return (element.GetString() ?? string.Empty).Trim();
    }

    private static string NormalizeSpecimenType(string value)
    {
          if (string.Equals(value, "Blood", StringComparison.OrdinalIgnoreCase))
        return "Blood";

    if (string.Equals(value, "Urine", StringComparison.OrdinalIgnoreCase))
        return "Urine";

    if (string.Equals(value, "Tissue", StringComparison.OrdinalIgnoreCase))
        return "Tissue";

    if (string.Equals(value, "Saliva", StringComparison.OrdinalIgnoreCase))
        return "Saliva";

    throw new InvalidOperationException("Unknown specimen type");

    }

    private static string NormalizePriority(string value)
    {
         if (string.Equals(value, "Routine", StringComparison.OrdinalIgnoreCase))
        return "Routine";

    if (string.Equals(value, "Urgent", StringComparison.OrdinalIgnoreCase))
        return "Urgent";

    throw new InvalidOperationException("Unknown priority");
    }

    private static List<string> GetRequestedTests(Dictionary<string, JsonElement> fields)
    {
        var element = fields["requestedTests"];
        var tests = new List<string>();

        foreach (var item in element.EnumerateArray())
        {
            tests.Add((item.GetString() ?? string.Empty).Trim());
        }

        return tests;
    }
}
