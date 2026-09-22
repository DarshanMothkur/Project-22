using System.Text.Json;

namespace OrderIntake;

public sealed class OrderProcessor
{
    public OrderResult Process(string json)
    {
        try
        {
            using var document = JsonOrderParser.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return RejectMalformed();
            }

            var fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                fields[property.Name] = property.Value;
            }

            foreach (var field in new[] { "orderId", "patientId", "specimenId", "specimenType", "priority", "collectionDate", "requestedTests" })
            {
                if (!fields.TryGetValue(field, out var element))
                {
                    continue;
                }

                if (field == "requestedTests")
                {
                    if (element.ValueKind != JsonValueKind.Array && element.ValueKind != JsonValueKind.Null)
                    {
                        return RejectMalformed();
                    }

                    continue;
                }

                if (element.ValueKind != JsonValueKind.String && element.ValueKind != JsonValueKind.Null)
                {
                    return RejectMalformed();
                }
            }

            var errors = OrderValidator.Validate(fields);
            if (errors.Count > 0)
            {
                return new OrderResult
                {
                    Status = OrderStatus.Rejected,
                    Order = null,
                    Errors = errors
                };
            }

            var order = OrderBuilder.Build(fields);
            return new OrderResult
            {
                Status = OrderStatus.Accepted,
                Order = order,
                Errors = new List<OrderError>()
            };
        }
        catch (MalformedInputException)
        {
            return RejectMalformed();
        }
        catch
        {
            return RejectMalformed();
        }
    }

    private static OrderResult RejectMalformed()
    {
        return new OrderResult
        {
            Status = OrderStatus.Rejected,
            Order = null,
            Errors = new List<OrderError>
            {
                new() {  Field = "$",
    Code = "MALFORMED_INPUT",
    Message = "Input is malformed."}
            }
        };
    }
}
