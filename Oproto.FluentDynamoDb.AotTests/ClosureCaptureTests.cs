using System;
using System.Linq.Expressions;
using Oproto.FluentDynamoDb.Expressions;
using Oproto.FluentDynamoDb.Requests;

namespace Oproto.FluentDynamoDb.AotTests;

/// <summary>
/// Tests for closure capture in AOT environments.
/// Verifies that local variables, fields, and nested closures work correctly.
/// </summary>
public static class ClosureCaptureTests
{
    private static string _staticField = "STATIC_VALUE";

    public static int Run()
    {
        Console.WriteLine("Running Closure Capture Tests...");
        int failures = 0;

        failures += TestLocalVariableCapture() ? 0 : 1;
        failures += TestFieldCapture() ? 0 : 1;
        failures += TestNestedClosureCapture() ? 0 : 1;
        failures += TestComplexClosureScenarios() ? 0 : 1;

        return failures;
    }

    private static bool TestLocalVariableCapture()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();

            // Test simple local variable capture
            string userId = "USER#123";
            Expression<Func<TestEntity, bool>> expr = x => x.PartitionKey == userId;
            
            string result = translator.Translate(expr, context);
            
            // Check that it contains attribute name and parameter placeholders
            bool hasAttr = result.Contains("#attr") || result.Contains("#pk");
            bool hasParam = result.Contains(":p");
            bool hasEquals = result.Contains("=");
            
            return TestHelpers.AssertTrue(
                "Local variable capture",
                hasAttr && hasParam && hasEquals);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Local variable capture - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestFieldCapture()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();

            // Test static field capture
            Expression<Func<TestEntity, bool>> expr = x => x.Name == _staticField;
            
            string result = translator.Translate(expr, context);
            
            // Check that it contains attribute name and parameter placeholders
            bool hasAttr = result.Contains("#attr") || result.Contains("#name");
            bool hasParam = result.Contains(":p");
            bool hasEquals = result.Contains("=");
            
            return TestHelpers.AssertTrue(
                "Static field capture",
                hasAttr && hasParam && hasEquals);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Static field capture - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestNestedClosureCapture()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();

            // Test nested closure - use simple property access instead of string concatenation
            // String concatenation is not supported in DynamoDB expressions
            var wrapper = new { UserId = "USER#123" };
            
            Expression<Func<TestEntity, bool>> expr = x => 
                x.PartitionKey == wrapper.UserId;
            
            string result = translator.Translate(expr, context);
            
            // Check that it contains attribute name and parameter placeholders
            bool hasAttr = result.Contains("#attr") || result.Contains("#pk");
            bool hasParam = result.Contains(":p");
            bool hasEquals = result.Contains("=");
            
            return TestHelpers.AssertTrue(
                "Nested closure capture",
                hasAttr && hasParam && hasEquals);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Nested closure capture - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestComplexClosureScenarios()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();

            // Test complex closure with multiple captures
            int minAge = 18;
            int maxAge = 65;
            var status = EntityStatus.Active;
            
            Expression<Func<TestEntity, bool>> expr = x => 
                x.Age >= minAge && x.Age <= maxAge && x.Status == status;
            
            string result = translator.Translate(expr, context);
            
            // Check for attribute placeholders, parameters, and AND operator
            bool hasAttr = result.Contains("#attr") || result.Contains("#age") || result.Contains("#status");
            bool hasParam = result.Contains(":p");
            bool hasAnd = result.Contains("AND");
            bool hasComparison = result.Contains(">=") && result.Contains("<=");
            
            return TestHelpers.AssertTrue(
                "Complex closure with multiple captures",
                hasAttr && hasParam && hasAnd && hasComparison);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Complex closure scenarios - Exception: {ex.Message}");
            return false;
        }
    }

    private static ExpressionContext CreateTestContext()
    {
        var attributeValues = new AttributeValueInternal();
        var attributeNames = new AttributeNameInternal();
        var paramGen = new ParameterGenerator();
        
        return new ExpressionContext(
            attributeValues,
            attributeNames,
            null,
            ExpressionValidationMode.None);
    }
}
