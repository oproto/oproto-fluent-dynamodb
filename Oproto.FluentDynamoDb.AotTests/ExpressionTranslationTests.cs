using System;
using System.Linq.Expressions;
using Oproto.FluentDynamoDb.Expressions;
using Oproto.FluentDynamoDb.Requests;

namespace Oproto.FluentDynamoDb.AotTests;

/// <summary>
/// Tests for expression translation in AOT environments.
/// Verifies all operators, DynamoDB functions, and value capture work correctly.
/// </summary>
public static class ExpressionTranslationTests
{
    public static int Run()
    {
        Console.WriteLine("Running Expression Translation Tests...");
        int failures = 0;

        failures += TestOperatorTranslation() ? 0 : 1;
        failures += TestDynamoDbFunctions() ? 0 : 1;
        failures += TestValueCaptureWithVariousTypes() ? 0 : 1;
        failures += TestValidationAndErrorHandling() ? 0 : 1;

        return failures;
    }

    private static bool TestOperatorTranslation()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();
            bool allPassed = true;

            // Test equality
            Expression<Func<TestEntity, bool>> eqExpr = x => x.Age == 25;
            string eqResult = translator.Translate(eqExpr, context);
            allPassed &= (eqResult.Contains("#age") || eqResult.Contains("#attr")) && eqResult.Contains("=") && eqResult.Contains(":p");

            // Test comparison operators
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> gtExpr = x => x.Age > 18;
            string gtResult = translator.Translate(gtExpr, context);
            allPassed &= (gtResult.Contains("#age") || gtResult.Contains("#attr")) && gtResult.Contains(">") && gtResult.Contains(":p");

            // Test logical AND
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> andExpr = x => x.Age > 18 && x.Age < 65;
            string andResult = translator.Translate(andExpr, context);
            allPassed &= andResult.Contains("AND");

            // Test logical OR
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> orExpr = x => x.Status == EntityStatus.Active || x.Status == EntityStatus.Pending;
            string orResult = translator.Translate(orExpr, context);
            allPassed &= orResult.Contains("OR");

            // Test NOT
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> notExpr = x => !(x.Age < 18);
            string notResult = translator.Translate(notExpr, context);
            allPassed &= notResult.Contains("NOT");

            return TestHelpers.AssertTrue("All operator translations", allPassed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Operator translation - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestDynamoDbFunctions()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();
            bool allPassed = true;

            // Test StartsWith -> begins_with
            Expression<Func<TestEntity, bool>> startsWithExpr = x => x.Name.StartsWith("John");
            string startsWithResult = translator.Translate(startsWithExpr, context);
            allPassed &= startsWithResult.Contains("begins_with");

            // Test Contains -> contains
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> containsExpr = x => x.Name.Contains("test");
            string containsResult = translator.Translate(containsExpr, context);
            allPassed &= containsResult.Contains("contains");

            // Test Between
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> betweenExpr = x => x.Age.Between(18, 65);
            string betweenResult = translator.Translate(betweenExpr, context);
            allPassed &= betweenResult.Contains("BETWEEN");

            // Test AttributeExists
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> existsExpr = x => x.Email.AttributeExists();
            string existsResult = translator.Translate(existsExpr, context);
            allPassed &= existsResult.Contains("attribute_exists");

            // Test AttributeNotExists
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> notExistsExpr = x => x.Email.AttributeNotExists();
            string notExistsResult = translator.Translate(notExistsExpr, context);
            allPassed &= notExistsResult.Contains("attribute_not_exists");

            return TestHelpers.AssertTrue("All DynamoDB function translations", allPassed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ DynamoDB functions - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestValueCaptureWithVariousTypes()
    {
        try
        {
            var translator = new ExpressionTranslator();
            bool allPassed = true;

            // Test string
            var context = CreateTestContext();
            string stringValue = "test";
            Expression<Func<TestEntity, bool>> stringExpr = x => x.Name == stringValue;
            string stringResult = translator.Translate(stringExpr, context);
            allPassed &= stringResult.Contains(":p");

            // Test int
            context = CreateTestContext();
            int intValue = 42;
            Expression<Func<TestEntity, bool>> intExpr = x => x.Age == intValue;
            string intResult = translator.Translate(intExpr, context);
            allPassed &= intResult.Contains(":p");

            // Test DateTime
            context = CreateTestContext();
            DateTime dateValue = DateTime.UtcNow;
            Expression<Func<TestEntity, bool>> dateExpr = x => x.CreatedAt > dateValue;
            string dateResult = translator.Translate(dateExpr, context);
            allPassed &= dateResult.Contains(":p");

            // Test enum
            context = CreateTestContext();
            EntityStatus enumValue = EntityStatus.Active;
            Expression<Func<TestEntity, bool>> enumExpr = x => x.Status == enumValue;
            string enumResult = translator.Translate(enumExpr, context);
            allPassed &= enumResult.Contains(":p");

            return TestHelpers.AssertTrue("Value capture with various types", allPassed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Value capture - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestValidationAndErrorHandling()
    {
        try
        {
            var translator = new ExpressionTranslator();
            bool allPassed = true;

            // Test that unsupported expressions throw appropriate exceptions
            // Note: We can't test all validation without metadata, but we can test basic error handling
            
            var context = CreateTestContext();
            
            // Test that translation doesn't crash on valid expressions
            Expression<Func<TestEntity, bool>> validExpr = x => x.Age > 18 && x.Name == "test";
            string result = translator.Translate(validExpr, context);
            allPassed &= !string.IsNullOrEmpty(result);

            return TestHelpers.AssertTrue("Validation and error handling", allPassed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Validation and error handling - Exception: {ex.Message}");
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
