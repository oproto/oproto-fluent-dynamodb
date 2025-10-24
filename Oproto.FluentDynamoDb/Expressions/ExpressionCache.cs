using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Oproto.FluentDynamoDb.Expressions;

/// <summary>
/// Cache for translated expressions to avoid repeated analysis.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
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
