using System.Globalization;
using System.Text.Json;

namespace OrderIntake;

internal static class OrderValidator
{
    private const string RequiredCode = "REQUIRED";
    private const string MaxLengthCode = "MAX_LENGTH";
    private const string InvalidValueCode = "INVALID_VALUE";
    private const string DuplicateCode = "DUPLICATE";
    private const string InvalidFormatCode = "INVALID_FORMAT";
    private const string FutureDateCode = "FUTURE_DATE";

    private static readonly Dictionary<string, string> SpecimenTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Blood"] = "Blood",
        ["Urine"] = "Urine",
        ["Tissue"] = "Tissue",
        ["Saliva"] = "Saliva"
    };

    private static readonly Dictionary<string, string> PriorityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Routine"] = "Routine",
        ["Urgent"] = "Urgent"
    };

    public static List<OrderError> Validate(Dictionary<string, JsonElement> fields)
    {
        var errors = new List<OrderError>();

        ValidateStringField(fields, "orderId", errors, maxLength: 20, allowEmpty: false);
        ValidateStringField(fields, "patientId", errors, maxLength: 20, allowEmpty: false);
        ValidateStringField(fields, "specimenId", errors, maxLength: 20, allowEmpty: false);

        ValidateEnumField(fields, "specimenType", errors, SpecimenTypeMap, "specimenType");
        ValidateEnumField(fields, "priority", errors, PriorityMap, "priority");
        ValidateCollectionDateField(fields, errors);
        ValidateRequestedTestsField(fields, errors);

        return errors;
    }

    private static void ValidateStringField(
        Dictionary<string, JsonElement> fields,
        string fieldName,
        List<OrderError> errors,
        int maxLength,
        bool allowEmpty)
    {
        if (!fields.TryGetValue(fieldName, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(CreateError(fieldName, RequiredCode));
            return;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new MalformedInputException();
        }

        var value = element.GetString() ?? string.Empty;
        if (!allowEmpty && string.IsNullOrWhiteSpace(value))
        {
            errors.Add(CreateError(fieldName, RequiredCode));
            return;
        }

        if (value.Length > maxLength)
        {
            errors.Add(CreateError(fieldName, MaxLengthCode));
        }
    }

    private static void ValidateEnumField(
        Dictionary<string, JsonElement> fields,
        string fieldName,
        List<OrderError> errors,
        Dictionary<string, string> validValues,
        string propertyName)
    {
        if (!fields.TryGetValue(fieldName, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(CreateError(fieldName, RequiredCode));
            return;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new MalformedInputException();
        }

        var value = (element.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(CreateError(fieldName, RequiredCode));
            return;
        }

        if (!validValues.TryGetValue(value, out var normalized))
        {
            errors.Add(CreateError(fieldName, InvalidValueCode));
        }
    }

    private static void ValidateCollectionDateField(Dictionary<string, JsonElement> fields, List<OrderError> errors)
    {
        if (!fields.TryGetValue("collectionDate", out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(CreateError("collectionDate", RequiredCode));
            return;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new MalformedInputException();
        }

        var text = element.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add(CreateError("collectionDate", RequiredCode));
            return;
        }

        if (!DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            errors.Add(CreateError("collectionDate", InvalidFormatCode));
            return;
        }

        if (date > DateOnly.FromDateTime(DateTime.Today))
        {
            errors.Add(CreateError("collectionDate", FutureDateCode));
        }
    }

    private static void ValidateRequestedTestsField(Dictionary<string, JsonElement> fields, List<OrderError> errors)
    {
        if (!fields.TryGetValue("requestedTests", out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(CreateError("requestedTests", RequiredCode));
            return;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new MalformedInputException();
        }

        var tests = element.EnumerateArray().ToList();
        if (tests.Count == 0)
        {
            errors.Add(CreateError("requestedTests", RequiredCode));
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateFound = false;

        foreach (var item in tests)
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw new MalformedInputException();
            }

            var value = (item.GetString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(CreateError("requestedTests", InvalidValueCode));
                continue;
            }

            if (!seen.Add(value))
            {
                duplicateFound = true;
            }
        }

        if (duplicateFound)
        {
            errors.Add(CreateError("requestedTests", DuplicateCode));
        }
    }

    private static OrderError CreateError(string field, string code)
    {
           var message = code switch
    {
        RequiredCode => $"{field} is required.",
        MaxLengthCode => $"{field} must be 20 characters or fewer.",
        InvalidValueCode => $"{field} has an invalid value.",
        DuplicateCode => $"{field} contains duplicate values.",
        InvalidFormatCode => $"{field} must be a valid yyyy-MM-dd date.",
        FutureDateCode => $"{field} cannot be a future date.",
        _ => "Invalid input."
    };

    return new OrderError
    {
        Field = field,
        Code = code,
        Message = message
    };
    }
}
