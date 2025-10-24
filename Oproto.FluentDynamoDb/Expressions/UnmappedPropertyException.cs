using System.Linq.Expressions;

namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Thrown when an expression references an unmapped property.
/// This occurs when a property in the expression doesn't have a corresponding DynamoDB attribute mapping.
/// </summary>
/// <remarks>
/// <para><strong>Common Causes:</strong></para>
/// <list type="bullet">
/// <item><description>Property is not decorated with [DynamoDbAttribute]</description></item>
/// <item><description>Property is not included in entity metadata</description></item>
/// <item><description>Property is a computed property without DynamoDB mapping</description></item>
/// <item><description>Typo in property name</description></item>
/// </list>
/// 
/// <para><strong>Resolution:</strong></para>
/// <list type="number">
/// <item><description>Add [DynamoDbAttribute] to the property</description></item>
/// <item><description>Include the property in entity configuration</description></item>
/// <item><description>Use a different property that is mapped</description></item>
/// <item><description>Use string-based expressions if property mapping is not possible</description></item>
/// </list>
/// 
/// <para><strong>Property Information:</strong></para>
/// <para>
/// The exception provides <see cref="PropertyName"/> and <see cref="EntityType"/> properties
/// to help identify which property and entity type caused the error.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Entity without proper attribute mapping
/// public class User
/// {
///     [DynamoDbAttribute("pk")]
///     public string PartitionKey { get; set; }
///     
///     // Missing [DynamoDbAttribute] - will cause UnmappedPropertyException
///     public string Email { get; set; }
/// }
/// 
/// try
/// {
///     table.Query
///         .Where&lt;User&gt;(x => x.PartitionKey == userId)
///         .WithFilter&lt;User&gt;(x => x.Email.Contains("@example.com"))
///         .ExecuteAsync();
/// }
/// catch (UnmappedPropertyException ex)
/// {
///     Console.WriteLine($"Property: {ex.PropertyName}"); // "Email"
///     Console.WriteLine($"Entity: {ex.EntityType.Name}"); // "User"
///     Console.WriteLine(ex.Message);
///     // "Property 'Email' on type 'User' does not map to a DynamoDB attribute..."
/// }
/// 
/// // Fix: Add attribute mapping
/// public class User
/// {
///     [DynamoDbAttribute("pk")]
///     public string PartitionKey { get; set; }
///     
///     [DynamoDbAttribute("email")]
///     public string Email { get; set; }
/// }
/// 
/// // Alternative: Use string-based expression
/// table.Query
///     .Where("pk = :pk")
///     .WithFilter("contains(#email, :domain)")
///     .WithAttribute("#email", "email")
///     .WithValue(":pk", userId)
///     .WithValue(":domain", "@example.com")
///     .ExecuteAsync();
/// </code>
/// </example>
public class UnmappedPropertyException : ExpressionTranslationException
{
    /// <summary>
    /// Gets the name of the property that is not mapped.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the entity type that contains the unmapped property.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmappedPropertyException"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the unmapped property.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="expression">The original expression.</param>
    public UnmappedPropertyException(string propertyName, Type entityType, Expression? expression = null)
        : base(
            $"Property '{propertyName}' on type '{entityType.Name}' does not map to a DynamoDB attribute. " +
            $"Ensure the property has a [DynamoDbAttribute] or is included in entity configuration.",
            expression)
    {
        PropertyName = propertyName;
        EntityType = entityType;
    }
}
