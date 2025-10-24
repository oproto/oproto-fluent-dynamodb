using System;
using System.Linq.Expressions;
using System.Reflection;
using Oproto.FluentDynamoDb.Expressions;
using Oproto.FluentDynamoDb.Requests;

namespace Oproto.FluentDynamoDb.AotTests;

/// <summary>
/// Tests for trimming compatibility in AOT environments.
/// Verifies that the trimmed binary works correctly and no required code is removed.
/// </summary>
public static class TrimmingCompatibilityTests
{
    public static int Run()
    {
        Console.WriteLine("Running Trimming Compatibility Tests...");
        int failures = 0;

        failures += TestCoreTypesPresent() ? 0 : 1;
        failures += TestExpressionTranslationWorks() ? 0 : 1;
        failures += TestExtensionMethodsWork() ? 0 : 1;

        return failures;
    }

    private static bool TestCoreTypesPresent()
    {
        try
        {
            // Verify that core types are present after trimming
            bool allPresent = true;

            // Check ExpressionTranslator
            var translatorType = typeof(ExpressionTranslator);
            allPresent &= translatorType != null;

            // Check ExpressionContext
            var contextType = typeof(ExpressionContext);
            allPresent &= contextType != null;

            // Check DynamoDbExpressionExtensions
            var extensionsType = typeof(DynamoDbExpressionExtensions);
            allPresent &= extensionsType != null;

            // Check that key methods exist
            var translateMethod = translatorType.GetMethod("Translate", BindingFlags.Public | BindingFlags.Instance);
            allPresent &= translateMethod != null;

            return TestHelpers.AssertTrue("Core types present after trimming", allPresent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Core types present - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestExpressionTranslationWorks()
    {
        try
        {
            // Verify that expression translation actually works in trimmed binary
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();

            // Test a variety of expressions to ensure nothing was trimmed
            bool allWork = true;

            // Simple comparison
            Expression<Func<TestEntity, bool>> expr1 = x => x.Age > 18;
            string result1 = translator.Translate(expr1, context);
            allWork &= !string.IsNullOrEmpty(result1);

            // Logical operators
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> expr2 = x => x.Age > 18 && x.Name == "test";
            string result2 = translator.Translate(expr2, context);
            allWork &= !string.IsNullOrEmpty(result2);

            // DynamoDB functions
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> expr3 = x => x.Name.StartsWith("John");
            string result3 = translator.Translate(expr3, context);
            allWork &= result3.Contains("begins_with");

            return TestHelpers.AssertTrue("Expression translation works in trimmed binary", allWork);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Expression translation works - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestExtensionMethodsWork()
    {
        try
        {
            // Verify that extension methods are available and work
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();
            bool allWork = true;

            // Test Between extension
            Expression<Func<TestEntity, bool>> betweenExpr = x => x.Age.Between(18, 65);
            string betweenResult = translator.Translate(betweenExpr, context);
            allWork &= betweenResult.Contains("BETWEEN");

            // Test AttributeExists extension
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> existsExpr = x => x.Email.AttributeExists();
            string existsResult = translator.Translate(existsExpr, context);
            allWork &= existsResult.Contains("attribute_exists");

            // Test AttributeNotExists extension
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> notExistsExpr = x => x.Email.AttributeNotExists();
            string notExistsResult = translator.Translate(notExistsExpr, context);
            allWork &= notExistsResult.Contains("attribute_not_exists");

            return TestHelpers.AssertTrue("Extension methods work in trimmed binary", allWork);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Extension methods work - Exception: {ex.Message}");
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
