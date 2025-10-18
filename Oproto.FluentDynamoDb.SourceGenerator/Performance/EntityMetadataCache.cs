using Oproto.FluentDynamoDb.SourceGenerator.Models;
using System.Collections.Concurrent;

namespace Oproto.FluentDynamoDb.SourceGenerator.Performance;

/// <summary>
/// High-performance cache for EntityMetadata to avoid repeated expensive operations.
/// Uses thread-safe concurrent collections and weak references to prevent memory leaks.
/// </summary>
public static class EntityMetadataCache
{
    private static readonly ConcurrentDictionary<string, WeakReference<EntityMetadata>> _cache = new();
    private static readonly ConcurrentDictionary<string, object> _locks = new();
    
    /// <summary>
    /// Gets or creates EntityMetadata for the specified entity type with caching.
    /// </summary>
    /// <param name="entityModel">The entity model to generate metadata for.</param>
    /// <returns>Cached or newly created EntityMetadata.</returns>
    public static EntityMetadata GetOrCreate(EntityModel entityModel)
    {
        var cacheKey = GetCacheKey(entityModel);
        
        // Fast path: try to get from cache first
        if (_cache.TryGetValue(cacheKey, out var weakRef) && 
            weakRef.TryGetTarget(out var cachedMetadata))
        {
            return cachedMetadata;
        }
        
        // Slow path: create new metadata with locking to prevent duplicate work
        var lockObject = _locks.GetOrAdd(cacheKey, _ => new object());
        
        lock (lockObject)
        {
            // Double-check pattern: another thread might have created it while we were waiting
            if (_cache.TryGetValue(cacheKey, out weakRef) && 
                weakRef.TryGetTarget(out cachedMetadata))
            {
                return cachedMetadata;
            }
            
            // Create new metadata
            var metadata = CreateEntityMetadata(entityModel);
            
            // Store in cache with weak reference to allow GC if needed
            _cache[cacheKey] = new WeakReference<EntityMetadata>(metadata);
            
            return metadata;
        }
    }
    
    /// <summary>
    /// Clears the metadata cache. Useful for testing or when memory pressure is high.
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
        _locks.Clear();
    }
    
    /// <summary>
    /// Gets cache statistics for monitoring and debugging.
    /// </summary>
    /// <returns>Cache statistics including hit rate and memory usage.</returns>
    public static CacheStatistics GetStatistics()
    {
        var totalEntries = _cache.Count;
        var aliveEntries = 0;
        
        foreach (var kvp in _cache)
        {
            if (kvp.Value.TryGetTarget(out _))
            {
                aliveEntries++;
            }
        }
        
        return new CacheStatistics
        {
            TotalEntries = totalEntries,
            AliveEntries = aliveEntries,
            DeadEntries = totalEntries - aliveEntries,
            MemoryEfficiency = totalEntries > 0 ? (double)aliveEntries / totalEntries : 1.0
        };
    }
    
    /// <summary>
    /// Performs cache maintenance by removing dead weak references.
    /// Called automatically but can be invoked manually for immediate cleanup.
    /// </summary>
    public static void Cleanup()
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _cache)
        {
            if (!kvp.Value.TryGetTarget(out _))
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
            _locks.TryRemove(key, out _);
        }
    }
    
    private static string GetCacheKey(EntityModel entityModel)
    {
        // Create a stable cache key based on entity characteristics
        // Include hash of properties, indexes, and relationships to detect changes
        var keyBuilder = new System.Text.StringBuilder();
        keyBuilder.Append(entityModel.Namespace);
        keyBuilder.Append('.');
        keyBuilder.Append(entityModel.ClassName);
        keyBuilder.Append('|');
        keyBuilder.Append(entityModel.TableName);
        keyBuilder.Append('|');
        keyBuilder.Append(entityModel.IsMultiItemEntity);
        
        // Include property signatures
        foreach (var prop in entityModel.Properties.OrderBy(p => p.PropertyName))
        {
            keyBuilder.Append('|');
            keyBuilder.Append(prop.PropertyName);
            keyBuilder.Append(':');
            keyBuilder.Append(prop.PropertyType);
            keyBuilder.Append(':');
            keyBuilder.Append(prop.AttributeName);
            keyBuilder.Append(':');
            keyBuilder.Append(prop.IsPartitionKey);
            keyBuilder.Append(':');
            keyBuilder.Append(prop.IsSortKey);
        }
        
        // Include index signatures
        foreach (var index in entityModel.Indexes.OrderBy(i => i.IndexName))
        {
            keyBuilder.Append("|IDX:");
            keyBuilder.Append(index.IndexName);
            keyBuilder.Append(':');
            keyBuilder.Append(index.PartitionKeyProperty);
            keyBuilder.Append(':');
            keyBuilder.Append(index.SortKeyProperty);
        }
        
        return keyBuilder.ToString();
    }
    
    private static EntityMetadata CreateEntityMetadata(EntityModel entityModel)
    {
        return new EntityMetadata
        {
            TableName = entityModel.TableName,
            EntityDiscriminator = entityModel.EntityDiscriminator,
            IsMultiItemEntity = entityModel.IsMultiItemEntity,
            Properties = entityModel.Properties
                .Where(p => p.HasAttributeMapping)
                .Select(CreatePropertyMetadata)
                .ToArray(),
            Indexes = entityModel.Indexes
                .Select(CreateIndexMetadata)
                .ToArray(),
            Relationships = entityModel.Relationships
                .Select(CreateRelationshipMetadata)
                .ToArray()
        };
    }
    
    private static PropertyMetadata CreatePropertyMetadata(PropertyModel property)
    {
        return new PropertyMetadata
        {
            PropertyName = property.PropertyName,
            AttributeName = property.AttributeName,
            PropertyType = GetTypeFromString(property.PropertyType),
            IsPartitionKey = property.IsPartitionKey,
            IsSortKey = property.IsSortKey,
            IsCollection = property.IsCollection,
            IsNullable = property.IsNullable,
            SupportedOperations = property.Queryable?.SupportedOperations ?? Array.Empty<DynamoDbOperation>(),
            AvailableInIndexes = property.Queryable?.AvailableInIndexes ?? Array.Empty<string>()
        };
    }
    
    private static IndexMetadata CreateIndexMetadata(IndexModel index)
    {
        return new IndexMetadata
        {
            IndexName = index.IndexName,
            PartitionKeyProperty = index.PartitionKeyProperty,
            SortKeyProperty = index.SortKeyProperty,
            ProjectedProperties = index.ProjectedProperties ?? Array.Empty<string>()
        };
    }
    
    private static RelationshipMetadata CreateRelationshipMetadata(RelationshipModel relationship)
    {
        return new RelationshipMetadata
        {
            PropertyName = relationship.PropertyName,
            SortKeyPattern = relationship.SortKeyPattern,
            EntityType = !string.IsNullOrEmpty(relationship.EntityType) ? GetTypeFromString(relationship.EntityType) : null,
            IsCollection = relationship.IsCollection
        };
    }
    
    private static Type GetTypeFromString(string typeName)
    {
        // This is a simplified type resolution for common types
        // In a real implementation, this would use the semantic model
        return typeName switch
        {
            "string" => typeof(string),
            "int" => typeof(int),
            "long" => typeof(long),
            "double" => typeof(double),
            "float" => typeof(float),
            "decimal" => typeof(decimal),
            "bool" => typeof(bool),
            "DateTime" => typeof(DateTime),
            "DateTimeOffset" => typeof(DateTimeOffset),
            "Guid" => typeof(Guid),
            "byte[]" => typeof(byte[]),
            _ => typeof(object) // Fallback for complex types
        };
    }
}

/// <summary>
/// Statistics about the EntityMetadata cache performance.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Total number of cache entries (including dead weak references).
    /// </summary>
    public int TotalEntries { get; set; }
    
    /// <summary>
    /// Number of cache entries with alive objects.
    /// </summary>
    public int AliveEntries { get; set; }
    
    /// <summary>
    /// Number of cache entries with dead weak references.
    /// </summary>
    public int DeadEntries { get; set; }
    
    /// <summary>
    /// Memory efficiency ratio (alive entries / total entries).
    /// </summary>
    public double MemoryEfficiency { get; set; }
    
    /// <summary>
    /// Indicates whether cache cleanup is recommended.
    /// </summary>
    public bool CleanupRecommended => MemoryEfficiency < 0.7 && TotalEntries > 10;
}