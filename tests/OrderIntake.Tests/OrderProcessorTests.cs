using System;
using System.Collections.Generic;
using System.Linq;
using OrderIntake;

namespace OrderIntake.Tests;

public class OrderProcessorTests
{
    [Fact]
    public void Process_AcceptedOrderWithMixedCaseValues_UsesNormalizedOrderAndNoErrors()
    {
        var json = @"{
  ""orderId"": ""ORD-1001"",
  ""patientId"": ""PAT-2002"",
  ""specimenId"": ""SPEC-3003"",
  ""specimenType"": ""blood"",
  ""priority"": ""urgent"",
  ""collectionDate"": ""2026-09-22"",
  ""requestedTests"": [""CBC"", ""CMP""],
  ""senderNote"": ""ignored""
}";

        var result = new OrderProcessor().Process(json);

        Assert.Equal(OrderStatus.Accepted, result.Status);
        Assert.NotNull(result.Order);
        Assert.Empty(result.Errors);
        Assert.Equal("ORD-1001", result.Order!.OrderId);
        Assert.Equal("PAT-2002", result.Order.PatientId);
        Assert.Equal("SPEC-3003", result.Order.SpecimenId);
        Assert.Equal("Blood", result.Order.SpecimenType);
        Assert.Equal("Urgent", result.Order.Priority);
        Assert.Equal(new DateOnly(2026, 9, 22), result.Order.CollectionDate);
        Assert.Equal(new[] { "CBC", "CMP" }, result.Order.RequestedTests);
    }

    [Fact]
    public void Process_AllValidationErrorsAtOnce_ReturnsExpectedFieldCodes()
    {
        var json = @"{
  ""orderId"": ""   "",
  ""patientId"": ""PAT-100"",
  ""specimenId"": ""SPEC-200"",
  ""specimenType"": ""Plasma"",
  ""priority"": ""Asap"",
  ""collectionDate"": ""2026-99-99"",
  ""requestedTests"": []
}";

        var result = new OrderProcessor().Process(json);

        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Null(result.Order);
        Assert.Contains(result.Errors, e => e.Field == "orderId" && e.Code == "REQUIRED");
        Assert.Contains(result.Errors, e => e.Field == "specimenType" && e.Code == "INVALID_VALUE");
        Assert.Contains(result.Errors, e => e.Field == "priority" && e.Code == "INVALID_VALUE");
        Assert.Contains(result.Errors, e => e.Field == "collectionDate" && e.Code == "INVALID_FORMAT");
        Assert.Contains(result.Errors, e => e.Field == "requestedTests" && e.Code == "REQUIRED");
    }

    [Theory]
    [InlineData("AAAAAAAAAAAAAAAAAAAA", true)]
    [InlineData("BBBBBBBBBBBBBBBBBBBBB", false)]
    public void Process_OrderIdMaxLength_ValidatesAsExpected(string orderId, bool accepted)
    {
        var json = $@"{{
  ""orderId"": ""{orderId}"",
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""{DateTime.Today:yyyy-MM-dd}"",
  ""requestedTests"": [""CBC""]
}}";

        var result = new OrderProcessor().Process(json);

        if (accepted)
        {
            Assert.Equal(OrderStatus.Accepted, result.Status);
            Assert.Empty(result.Errors);
        }
        else
        {
            Assert.Equal(OrderStatus.Rejected, result.Status);
            Assert.Contains(result.Errors, e => e.Field == "orderId" && e.Code == "MAX_LENGTH");
        }
    }

    [Fact]
    public void Process_CollectionDateValidation_RejectsInvalidFormatAndFutureDate()
    {
        var invalidFormatJson = @"{
  ""orderId"": ""ORD-1"",
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""2026-9-2"",
  ""requestedTests"": [""CBC""]
}";

        var invalidCalendarJson = @"{
  ""orderId"": ""ORD-2"",
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""2026-02-30"",
  ""requestedTests"": [""CBC""]
}";

        var futureDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        var futureJson = $@"{{
  ""orderId"": ""ORD-3"",
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""{futureDate}"",
  ""requestedTests"": [""CBC""]
}}";

        var invalidFormatResult = new OrderProcessor().Process(invalidFormatJson);
        var invalidCalendarResult = new OrderProcessor().Process(invalidCalendarJson);
        var futureResult = new OrderProcessor().Process(futureJson);

        Assert.Equal(OrderStatus.Rejected, invalidFormatResult.Status);
        Assert.Contains(invalidFormatResult.Errors, e => e.Field == "collectionDate" && e.Code == "INVALID_FORMAT");

        Assert.Equal(OrderStatus.Rejected, invalidCalendarResult.Status);
        Assert.Contains(invalidCalendarResult.Errors, e => e.Field == "collectionDate" && e.Code == "INVALID_FORMAT");

        Assert.Equal(OrderStatus.Rejected, futureResult.Status);
        Assert.Contains(futureResult.Errors, e => e.Field == "collectionDate" && e.Code == "FUTURE_DATE");
    }

    [Fact]
    public void Process_RequestedTestsValidation_RejectsEmptyDuplicateAndPreservesCasing()
    {
        var emptyJson = @"{
  ""orderId"": ""ORD-1"",
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""2026-09-22"",
  ""requestedTests"": []
}";

        var duplicateJson = @"{
  ""orderId"": ""ORD-2"",
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""2026-09-22"",
  ""requestedTests"": [""cbc"", ""CBC"", ""Chemistry""]
}";

        var validJson = @"{
  ""orderId"": ""ORD-3"",
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""2026-09-22"",
  ""requestedTests"": [""Hgb"", ""LFT""]
}";

        var emptyResult = new OrderProcessor().Process(emptyJson);
        var duplicateResult = new OrderProcessor().Process(duplicateJson);
        var validResult = new OrderProcessor().Process(validJson);

        Assert.Equal(OrderStatus.Rejected, emptyResult.Status);
        Assert.Contains(emptyResult.Errors, e => e.Field == "requestedTests" && e.Code == "REQUIRED");

        Assert.Equal(OrderStatus.Rejected, duplicateResult.Status);
        Assert.Contains(duplicateResult.Errors, e => e.Field == "requestedTests" && e.Code == "DUPLICATE");

        Assert.Equal(OrderStatus.Accepted, validResult.Status);
        Assert.Equal(new[] { "Hgb", "LFT" }, validResult.Order!.RequestedTests);
    }

    [Fact]
    public void Process_BrokenJson_ReturnsOneMalformedInputErrorAndDoesNotThrow()
    {
        var json = "{ ";

        var exception = Record.Exception(() => new OrderProcessor().Process(json));

        Assert.Null(exception);
        var result = new OrderProcessor().Process(json);
        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Null(result.Order);
        Assert.Single(result.Errors);
        Assert.Equal("$", result.Errors[0].Field);
        Assert.Equal("MALFORMED_INPUT", result.Errors[0].Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Process_InvalidInputShapes_ReturnMalformedInput(string? json)
    {
        var result = new OrderProcessor().Process(json!);

        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Null(result.Order);
        Assert.Single(result.Errors);
        Assert.Equal("$", result.Errors[0].Field);
        Assert.Equal("MALFORMED_INPUT", result.Errors[0].Code);
    }

    [Fact]
    public void Process_EmptyObject_ProducesFieldLevelValidationErrorsOnly()
    {
        var result = new OrderProcessor().Process("{}");

        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Null(result.Order);
        Assert.Contains(result.Errors, e => e.Field == "orderId" && e.Code == "REQUIRED");
        Assert.Contains(result.Errors, e => e.Field == "patientId" && e.Code == "REQUIRED");
        Assert.Contains(result.Errors, e => e.Field == "specimenId" && e.Code == "REQUIRED");
        Assert.Contains(result.Errors, e => e.Field == "specimenType" && e.Code == "REQUIRED");
        Assert.Contains(result.Errors, e => e.Field == "priority" && e.Code == "REQUIRED");
        Assert.Contains(result.Errors, e => e.Field == "collectionDate" && e.Code == "REQUIRED");
        Assert.Contains(result.Errors, e => e.Field == "requestedTests" && e.Code == "REQUIRED");
    }

    [Fact]
    public void Process_ArrayAndStringTopLevelJson_AreMalformed()
    {
        var arrayResult = new OrderProcessor().Process("[\"one\"]");
        var stringResult = new OrderProcessor().Process("\"hello\"");

        Assert.Equal(OrderStatus.Rejected, arrayResult.Status);
        Assert.Single(arrayResult.Errors);
        Assert.Equal("$", arrayResult.Errors[0].Field);
        Assert.Equal("MALFORMED_INPUT", arrayResult.Errors[0].Code);

        Assert.Equal(OrderStatus.Rejected, stringResult.Status);
        Assert.Single(stringResult.Errors);
        Assert.Equal("$", stringResult.Errors[0].Field);
        Assert.Equal("MALFORMED_INPUT", stringResult.Errors[0].Code);
    }

    [Fact]
    public void Process_IncompatibleRecognizedFieldType_IsMalformedInput()
    {
        var json = @"{
  ""orderId"": 123,
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""2026-09-22"",
  ""requestedTests"": [""CBC""]
}";

        var result = new OrderProcessor().Process(json);

        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Null(result.Order);
        Assert.Single(result.Errors);
        Assert.Equal("$", result.Errors[0].Field);
        Assert.Equal("MALFORMED_INPUT", result.Errors[0].Code);
    }

    [Fact]
    public void Process_ExplicitNullRequiredField_ProducesRequiredError()
    {
        var json = @"{
  ""orderId"": null,
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""Blood"",
  ""priority"": ""Routine"",
  ""collectionDate"": ""2026-09-22"",
  ""requestedTests"": [""CBC""]
}";

        var result = new OrderProcessor().Process(json);

        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Contains(result.Errors, e => e.Field == "orderId" && e.Code == "REQUIRED");
    }

    [Fact]
    public void Process_UnknownFields_AreIgnored()
    {
        var json = @"{
  ""orderId"": ""ORD-1"",
  ""patientId"": ""PAT-1"",
  ""specimenId"": ""SPEC-1"",
  ""specimenType"": ""tissue"",
  ""priority"": ""routine"",
  ""collectionDate"": ""2026-09-22"",
  ""requestedTests"": [""CBC""],
  ""senderNote"": ""ignored"",
  ""extra"": 123
}";

        var result = new OrderProcessor().Process(json);

        Assert.Equal(OrderStatus.Accepted, result.Status);
        Assert.NotNull(result.Order);
        Assert.Equal("Tissue", result.Order!.SpecimenType);
        Assert.Equal("Routine", result.Order.Priority);
    }
}
