# Laboratory Order Intake and Input Validation Service

## Description

This project implements a C# Laboratory Order Intake and Input Validation Service.

The service exposes a public `Process(string json)` method that accepts one laboratory order as a JSON string and returns one `OrderResult` object.

The service first parses and validates the JSON input. A valid JSON object is converted into a strongly typed `Order` object. If validation succeeds, the service returns `Accepted`, the strongly typed order, and an empty error list. If validation fails, the service returns `Rejected`, no order, and all validation errors found in the input.

Malformed JSON, a non-object top-level JSON value such as an array or string, or an incompatible type for a recognized field is treated as malformed input. In these cases the service returns exactly one error with field `$` and error code `MALFORMED_INPUT`, without throwing an unhandled exception.

Unknown JSON fields are ignored.

## Validation Rules

The service validates the following requirements:

- `orderId`, `patientId`, and `specimenId` are required.
- These identifiers must not be null, empty, or whitespace.
- Each identifier must be no longer than 20 characters.
- `specimenType` is required and accepts `Blood`, `Urine`, `Tissue`, or `Saliva`.
- `specimenType` matching is case-insensitive and is normalized to the fixed values `Blood`, `Urine`, `Tissue`, or `Saliva`.
- `priority` is required and accepts `Routine` or `Urgent`.
- `priority` matching is case-insensitive and is normalized to `Routine` or `Urgent`.
- `collectionDate` is required and must use the exact `yyyy-MM-dd` format.
- Invalid calendar dates are rejected.
- A collection date after the current date is rejected.
- `requestedTests` is required and must contain at least one item.
- Requested test items must not be empty or whitespace.
- Duplicate requested tests are detected case-insensitively.
- Original requested-test casing is preserved in the resulting order.
- Missing and explicit `null` required fields produce `REQUIRED` validation errors.
- Validation errors contain the field name, error code, and short message.
- Multiple validation errors are collected and returned together.

Input strings are trimmed before the relevant validation and order-building operations.

## Processing Design

The implementation is separated into simple components:

- `OrderProcessor` coordinates the complete processing flow and exposes `Process(string json)`.
- `JsonOrderParser` parses the JSON input and handles malformed JSON.
- `OrderValidator` validates all required fields and collects validation errors.
- `OrderBuilder` converts validated JSON data into the strongly typed `Order`.
- `Models` contains the `Order`, `OrderResult`, `OrderError`, and `OrderStatus` types.

The `Order` model uses a real `DateOnly` type for `CollectionDate` rather than storing the raw JSON date string.

The implementation does not use dependency injection, a web framework, database, console UI, or global state.

## Error Handling

Normal validation failures return `Rejected` with all applicable errors.

Malformed input returns:

- Status: `Rejected`
- Order: `null`
- Exactly one error
- Field: `$`
- Code: `MALFORMED_INPUT`
- Short error message

The service is designed so malformed input does not result in an unhandled exception.

## Tests

The project uses xUnit automated tests.

The tests cover:

- Accepted orders with mixed-case specimen type and priority.
- Normalization of accepted values.
- Ignoring unknown fields.
- Multiple validation errors in one request.
- Identifier length boundaries of 20 and 21 characters.
- Invalid collection dates.
- Future collection dates.
- Empty requested-test lists.
- Empty requested-test items.
- Case-insensitive duplicate requested tests.
- Broken JSON.
- Empty, null, and whitespace input.
- Empty JSON objects.
- Non-object top-level JSON values.
- Incompatible recognized-field types.
- Explicit null required fields.

The project was verified using:

`dotnet build`

and

`dotnet test`

with all automated tests passing and the build completing with zero warnings and zero errors.

## Build and Test

From the project root:

```text
dotnet build
dotnet test# Project-22