using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Cache for translated expressions to avoid repeated analysis.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
/// <remarks>
/// <para><strong>Caching Strategy:</strong></para>
/// <para>
/// The cache stores expression structure templates, not parameter values. This means that
/// expressions with the same structure but different values benefit from caching:
/// </para>
/// <code>
/// // First call - translates and caches
/// translator.TranslateWithCache(x => x.Id == userId1, context);
/// 
/// // Second call - uses cached structure, only values differ
/// translator.TranslateWithCache(x => x.Id == userId2, context);
/// </code>
/// 
/// <para><strong>Cache Key:</strong></para>
/// <para>
/// The cache key combines the expression structure (using ToString()) and the validation mode.
/// This ensures that the same expression used in different contexts (Query vs Filter) is
/// cached separately.
/// </para>
/// 
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// The cache is thread-safe and can be safely accessed from multiple threads concurrently.
/// It uses <see cref="ConcurrentDictionary{TKey,TValue}"/> internally for lock-free reads
/// and writes.
/// </para>
/// 
/// <para><strong>Performance Benefits:</strong></para>
/// <list type="bullet">
/// <item><description>Avoids repeated expression tree traversal</description></item>
/// <item><description>Reduces allocations for expression string building</description></item>
/// <item><description>Improves performance for frequently-used query patterns</description></item>
/// <item><description>Particularly beneficial in high-throughput scenarios</description></item>
/// </list>
/// 
/// <para><strong>Memory Considerations:</strong></para>
/// <para>
/// The cache grows unbounded by default. In long-running applications with many unique
/// expression patterns, consider periodically calling <see cref="Clear"/> to free memory.
/// Each cached entry stores only the expression string template (typically &lt; 1KB).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Access the global cache
/// var cache = ExpressionTranslator.Cache;
/// 
/// // Check cache size
/// Console.WriteLine($"Cached expressions: {cache.Count}");
/// 
/// // Clear cache if needed (e.g., after configuration changes)
/// cache.Clear();
/// 
/// // Use caching in translation
/// var translator = new ExpressionTranslator();
/// var result = translator.TranslateWithCache(x => x.Id == userId, context);
/// // Subsequent calls with same expression structure use cache
/// </code>
/// </example>
public class ExpressionCache
{
    private readonly ConcurrentDictionary<ExpressionCacheKey, string> _cache = new();

    /// <summary>
    /// Gets a cached expression translation or adds a new one using the provided translator function.
    /// </summary>
    /// <param name="expression">The expression to translate.</param>
    /// <param name="mode">The validation mode for the expression.</param>
    /// <param name="translator">Function to translate the expression if not cached.</param>
    /// <returns>The translated expression string.</returns>
    public string GetOrAdd(
        Expression expression,
        ExpressionValidationMode mode,
        Func<string> translator)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));
        if (translator == null)
            throw new ArgumentNullException(nameof(translator));

        var key = new ExpressionCacheKey(expression, mode);
        return _cache.GetOrAdd(key, _ => translator());
    }

    /// <summary>
    /// Clears all cached expressions.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets the number of cached expressions.
    /// </summary>
    public int Count => _cache.Count;
}

/// <summary>
/// Cache key for expression translations.
/// Combines the expression and validation mode to create a unique key.
/// </summary>
internal sealed record ExpressionCacheKey
{
    private readonly Expression _expression;
    private readonly ExpressionValidationMode _mode;
    private readonly int _hashCode;

    public ExpressionCacheKey(Expression expression, ExpressionValidationMode mode)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _mode = mode;
        
        // Pre-compute hash code for performance
        // Use expression's ToString() for structural comparison
        _hashCode = HashCode.Combine(_expression.ToString(), _mode);
    }

    public bool Equals(ExpressionCacheKey? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        // Compare validation mode first (fast)
        if (_mode != other._mode)
            return false;

        // Compare expression structure using ToString()
        // This provides structural equality without deep tree comparison
        return _expression.ToString() == other._expression.ToString();
    }

    public override int GetHashCode() => _hashCode;
}
