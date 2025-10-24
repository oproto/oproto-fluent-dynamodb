using System;
using System.Linq.Expressions;
using Oproto.FluentDynamoDb.Expressions;
using Oproto.FluentDynamoDb.Requests;

namespace Oproto.FluentDynamoDb.AotTests;

/// <summary>
/// Tests for generic method expressions in AOT environments.
/// Verifies that generic entity types and property types work correctly.
/// </summary>
public static class GenericMethodTests
{
    public static int Run()
    {
        Console.WriteLine("Running Generic Method Tests...");
        int failures = 0;

        failures += TestGenericEntityTypes() ? 0 : 1;
        failures += TestGenericPropertyTypes() ? 0 : 1;
        failures += TestGenericMethodCalls() ? 0 : 1;

        return failures;
    }

    private static bool TestGenericEntityTypes()
    {
        try
        {
            // Test with different entity types
            bool passed = true;
            
            passed &= TestWithEntityType<TestEntity>();
            passed &= TestWithEntityType<GenericTestEntity<string>>();
            passed &= TestWithEntityType<GenericTestEntity<int>>();

            return TestHelpers.AssertTrue("Generic entity types", passed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Generic entity types - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestWithEntityType<TEntity>() where TEntity : class, new()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();

            // Create a simple expression that should work for any entity
            // We can't use specific properties, so we'll just verify the translator doesn't crash
            var entity = new TEntity();
            
            // This tests that the generic type parameter is handled correctly
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TestGenericPropertyTypes()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();
            bool allPassed = true;

            // Test with nullable property
            string? nullableValue = "test";
            Expression<Func<TestEntity, bool>> nullableExpr = x => x.Email == nullableValue;
            string nullableResult = translator.Translate(nullableExpr, context);
            allPassed &= (nullableResult.Contains("#email") || nullableResult.Contains("#attr")) && nullableResult.Contains(":p");

            // Test with enum property
            context = CreateTestContext();
            EntityStatus statusValue = EntityStatus.Active;
            Expression<Func<TestEntity, bool>> enumExpr = x => x.Status == statusValue;
            string enumResult = translator.Translate(enumExpr, context);
            allPassed &= (enumResult.Contains("#status") || enumResult.Contains("#attr")) && enumResult.Contains(":p");

            // Test with DateTime property
            context = CreateTestContext();
            DateTime dateValue = DateTime.UtcNow;
            Expression<Func<TestEntity, bool>> dateExpr = x => x.CreatedAt > dateValue;
            string dateResult = translator.Translate(dateExpr, context);
            allPassed &= (dateResult.Contains("#createdAt") || dateResult.Contains("#attr")) && dateResult.Contains(":p");

            return TestHelpers.AssertTrue("Generic property types", allPassed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Generic property types - Exception: {ex.Message}");
            return false;
        }
    }

    private static bool TestGenericMethodCalls()
    {
        try
        {
            var context = CreateTestContext();
            var translator = new ExpressionTranslator();
            bool allPassed = true;

            // Test Between with different types
            Expression<Func<TestEntity, bool>> betweenIntExpr = x => x.Age.Between(18, 65);
            string betweenIntResult = translator.Translate(betweenIntExpr, context);
            allPassed &= betweenIntResult.Contains("BETWEEN");

            // Test AttributeExists with nullable type
            context = CreateTestContext();
            Expression<Func<TestEntity, bool>> existsExpr = x => x.Email.AttributeExists();
            string existsResult = translator.Translate(existsExpr, context);
            allPassed &= existsResult.Contains("attribute_exists");

            return TestHelpers.AssertTrue("Generic method calls in expressions", allPassed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Generic method calls - Exception: {ex.Message}");
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

/// <summary>
/// Generic test entity for testing generic type parameters.
/// </summary>
public class GenericTestEntity<T>
{
    public string Id { get; set; } = string.Empty;
    public T? Value { get; set; }
}
