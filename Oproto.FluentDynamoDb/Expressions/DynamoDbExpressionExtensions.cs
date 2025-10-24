namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Extension methods for DynamoDB expression support.
/// These methods are markers for expression translation and should not be called directly.
/// They will be recognized by the ExpressionTranslator and converted to DynamoDB syntax.
/// </summary>
public static class DynamoDbExpressionExtensions
{
    /// <summary>
    /// Generates a BETWEEN condition in DynamoDB expressions.
    /// This method is only for use in lambda expressions and will be translated to DynamoDB syntax.
    /// </summary>
    /// <typeparam name="T">The type of the value being compared.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="low">The lower bound (inclusive).</param>
    /// <param name="high">The upper bound (inclusive).</param>
    /// <returns>Always throws an exception if called directly.</returns>
    /// <exception cref="InvalidOperationException">Always thrown - this method is only for use in expressions.</exception>
    /// <example>
    /// <code>
    /// // In an expression:
    /// table.Query.Where&lt;MyEntity&gt;(x => x.Age.Between(18, 65))
    /// // Translates to: #age BETWEEN :p0 AND :p1
    /// </code>
    /// </example>
    [ExpressionOnly]
    public static bool Between<T>(this T value, T low, T high) where T : IComparable<T>
        => throw new InvalidOperationException("This method is only for use in expressions and should not be called directly.");
    
    /// <summary>
    /// Generates an attribute_exists() function in DynamoDB expressions.
    /// This method is only for use in lambda expressions and will be translated to DynamoDB syntax.
    /// </summary>
    /// <typeparam name="T">The type of the attribute.</typeparam>
    /// <param name="value">The attribute to check for existence.</param>
    /// <returns>Always throws an exception if called directly.</returns>
    /// <exception cref="InvalidOperationException">Always thrown - this method is only for use in expressions.</exception>
    /// <example>
    /// <code>
    /// // In an expression:
    /// table.Query.WithFilter&lt;MyEntity&gt;(x => x.OptionalField.AttributeExists())
    /// // Translates to: attribute_exists(#optionalField)
    /// </code>
    /// </example>
    [ExpressionOnly]
    public static bool AttributeExists<T>(this T value)
        => throw new InvalidOperationException("This method is only for use in expressions and should not be called directly.");
    
    /// <summary>
    /// Generates an attribute_not_exists() function in DynamoDB expressions.
    /// This method is only for use in lambda expressions and will be translated to DynamoDB syntax.
    /// </summary>
    /// <typeparam name="T">The type of the attribute.</typeparam>
    /// <param name="value">The attribute to check for non-existence.</param>
    /// <returns>Always throws an exception if called directly.</returns>
    /// <exception cref="InvalidOperationException">Always thrown - this method is only for use in expressions.</exception>
    /// <example>
    /// <code>
    /// // In an expression:
    /// table.Query.WithFilter&lt;MyEntity&gt;(x => x.OptionalField.AttributeNotExists())
    /// // Translates to: attribute_not_exists(#optionalField)
    /// </code>
    /// </example>
    [ExpressionOnly]
    public static bool AttributeNotExists<T>(this T value)
        => throw new InvalidOperationException("This method is only for use in expressions and should not be called directly.");
    
    /// <summary>
    /// Generates a size() function in DynamoDB expressions.
    /// This method is only for use in lambda expressions and will be translated to DynamoDB syntax.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to get the size of.</param>
    /// <returns>Always throws an exception if called directly.</returns>
    /// <exception cref="InvalidOperationException">Always thrown - this method is only for use in expressions.</exception>
    /// <example>
    /// <code>
    /// // In an expression:
    /// table.Query.WithFilter&lt;MyEntity&gt;(x => x.Items.Size() > 5)
    /// // Translates to: size(#items) > :p0
    /// </code>
    /// </example>
    [ExpressionOnly]
    public static int Size<T>(this IEnumerable<T> collection)
        => throw new InvalidOperationException("This method is only for use in expressions and should not be called directly.");
}

/// <summary>
/// Marks methods that are only valid within expression trees.
/// These methods should never be called directly at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ExpressionOnlyAttribute : Attribute
{
}
