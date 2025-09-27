namespace Oproto.FluentDynamoDb.Requests.Interfaces;

/// <summary>
/// Interface for request builders that support condition expressions.
/// Condition expressions allow you to specify conditions that must be met for the operation to succeed.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder implementing this interface.</typeparam>
public interface IWithConditionExpression<out TBuilder>
{
    /// <summary>
    /// Specifies a condition expression that must be satisfied for the operation to succeed.
    /// Use attribute name parameters (e.g., "#name") and value parameters (e.g., ":value") in expressions.
    /// </summary>
    /// <param name="conditionExpression">The condition expression to evaluate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Only update if the item exists and status is active
    /// .Where("attribute_exists(id) AND #status = :status")
    /// .WithAttribute("#status", "status")
    /// .WithValue(":status", "ACTIVE")
    /// </code>
    /// </example>
    public TBuilder Where(string conditionExpression);
}