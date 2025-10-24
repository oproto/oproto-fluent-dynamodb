using System;

namespace Oproto.FluentDynamoDb.Attributes;

/// <summary>
/// Marks a DynamoDB table class as supporting scan operations.
/// 
/// When applied to a table class, the source generator will create Scan() methods
/// that allow scanning all items in the table or index.
/// </summary>
/// <remarks>
/// <para>
/// <strong>⚠️ WARNING: Scan operations are expensive and should be used sparingly.</strong>
/// </para>
/// <para>
/// Scan operations consume read capacity for every item examined in the table,
/// not just the items that match your filter expression. This means:
/// </para>
/// <list type="bullet">
/// <item><description>A scan of a 10,000 item table consumes capacity for all 10,000 items</description></item>
/// <item><description>Filter expressions reduce data transfer but NOT consumed capacity</description></item>
/// <item><description>Scans can be slow and expensive on large tables</description></item>
/// <item><description>Scans can throttle other operations if capacity is limited</description></item>
/// </list>
/// <para>
/// <strong>Consider using Query operations instead whenever possible.</strong>
/// Query operations are more efficient because they use indexes to locate items directly.
/// </para>
/// <para>
/// Appropriate use cases for scan operations:
/// </para>
/// <list type="bullet">
/// <item><description>Data migration or ETL processes on small to medium tables</description></item>
/// <item><description>Analytics or reporting on tables with fewer than 1,000 items</description></item>
/// <item><description>Batch operations where you truly need to examine every item</description></item>
/// <item><description>One-time administrative tasks that can tolerate high latency</description></item>
/// <item><description>Development and testing scenarios with small datasets</description></item>
/// </list>
/// </remarks>
/// <example>
/// <para>Basic usage with the Scannable attribute:</para>
/// <code>
/// using Oproto.FluentDynamoDb.Attributes;
/// using Oproto.FluentDynamoDb.Storage;
/// 
/// [DynamoDbTable("Users")]
/// [Scannable]
/// public partial class UsersTable : DynamoDbTableBase
/// {
///     // Scan() methods will be generated automatically by the source generator
/// }
/// 
/// // Usage - parameterless scan:
/// var allUsers = await usersTable.Scan()
///     .ExecuteAsync();
/// 
/// // Usage - scan with filter expression:
/// var activeUsers = await usersTable.Scan("status = {0}", "ACTIVE")
///     .ExecuteAsync();
/// 
/// // Usage - scan with complex filter:
/// var recentUsers = await usersTable.Scan()
///     .WithFilter("createdAt > {0} AND accountType = {1}", 
///         DateTime.UtcNow.AddDays(-30), 
///         "PREMIUM")
///     .ExecuteAsync();
/// </code>
/// <para>Manual implementation without source generation:</para>
/// <code>
/// using Oproto.FluentDynamoDb.Storage;
/// using Oproto.FluentDynamoDb.Requests;
/// 
/// public class UsersTable : DynamoDbTableBase
/// {
///     public ScanRequestBuilder Scan() => 
///         new ScanRequestBuilder(DynamoDbClient, Logger).ForTable(Name);
///     
///     public ScanRequestBuilder Scan(string filterExpression, params object[] values)
///     {
///         var builder = Scan();
///         return Requests.Extensions.WithFilterExpressionExtensions
///             .WithFilter(builder, filterExpression, values);
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ScannableAttribute : Attribute
{
}
