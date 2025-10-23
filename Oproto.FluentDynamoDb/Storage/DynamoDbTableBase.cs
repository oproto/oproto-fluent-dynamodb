using Amazon.DynamoDBv2;
using Oproto.FluentDynamoDb.Logging;
using Oproto.FluentDynamoDb.Requests;

namespace Oproto.FluentDynamoDb.Storage;

/// <summary>
/// Base implementation for DynamoDB table abstraction
/// </summary>
public abstract class DynamoDbTableBase : IDynamoDbTable
{
    /// <summary>
    /// Initializes a new instance of the DynamoDbTableBase class.
    /// </summary>
    /// <param name="client">The DynamoDB client.</param>
    /// <param name="tableName">The name of the table.</param>
    public DynamoDbTableBase(IAmazonDynamoDB client, string tableName)
        : this(client, tableName, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DynamoDbTableBase class with optional logger.
    /// </summary>
    /// <param name="client">The DynamoDB client.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="logger">Optional logger for DynamoDB operations. If null, uses a no-op logger.</param>
    public DynamoDbTableBase(IAmazonDynamoDB client, string tableName, IDynamoDbLogger? logger)
        : this(client, tableName, logger, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DynamoDbTableBase class with optional logger and field encryptor.
    /// </summary>
    /// <param name="client">The DynamoDB client.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="logger">Optional logger for DynamoDB operations. If null, uses a no-op logger.</param>
    /// <param name="fieldEncryptor">Optional field encryptor for encrypting sensitive properties. If null, encryption is disabled.</param>
    public DynamoDbTableBase(IAmazonDynamoDB client, string tableName, IDynamoDbLogger? logger, IFieldEncryptor? fieldEncryptor)
    {
        DynamoDbClient = client;
        Name = tableName;
        Logger = logger ?? NoOpLogger.Instance;
        FieldEncryptor = fieldEncryptor;
    }

    public IAmazonDynamoDB DynamoDbClient { get; private init; }
    public string Name { get; private init; }
    
    /// <summary>
    /// Gets the logger for DynamoDB operations.
    /// </summary>
    protected IDynamoDbLogger Logger { get; private init; }

    /// <summary>
    /// Gets the field encryptor for encrypting and decrypting sensitive properties.
    /// Returns null if encryption is not configured for this table.
    /// </summary>
    protected IFieldEncryptor? FieldEncryptor { get; private init; }

    /// <summary>
    /// Gets the current encryption context identifier, checking both operation-specific and ambient contexts.
    /// This context is used by the field encryptor to determine the appropriate encryption key.
    /// </summary>
    /// <returns>The current encryption context identifier, or null if not set.</returns>
    /// <remarks>
    /// The encryption context can be set using EncryptionContext.Current or per-operation
    /// using WithEncryptionContext() on request builders. The per-operation context takes
    /// precedence over the ambient context.
    /// </remarks>
    protected string? GetEncryptionContext()
    {
        return EncryptionContext.GetEffectiveContext();
    }

    /// <summary>
    /// Gets a builder for GetItem operations on this table.
    /// </summary>
    public GetItemRequestBuilder Get => new GetItemRequestBuilder(DynamoDbClient).ForTable(Name);

    /// <summary>
    /// Gets a builder for UpdateItem operations on this table.
    /// </summary>
    public UpdateItemRequestBuilder Update => new UpdateItemRequestBuilder(DynamoDbClient).ForTable(Name);

    /// <summary>
    /// Gets a builder for Query operations on this table.
    /// </summary>
    public QueryRequestBuilder Query => new QueryRequestBuilder(DynamoDbClient).ForTable(Name);

    /// <summary>
    /// Gets a builder for PutItem operations on this table.
    /// </summary>
    public PutItemRequestBuilder Put => new PutItemRequestBuilder(DynamoDbClient).ForTable(Name);

    /// <summary>
    /// Gets a builder for DeleteItem operations on this table.
    /// Use this to delete individual items by their primary key, with optional condition expressions.
    /// </summary>
    public DeleteItemRequestBuilder Delete => new DeleteItemRequestBuilder(DynamoDbClient).ForTable(Name);

    /// <summary>
    /// Returns a scannable interface that provides access to scan operations.
    /// 
    /// This method implements intentional friction to discourage accidental scan usage.
    /// Scan operations are expensive and should only be used for legitimate use cases such as:
    /// - Data migration or ETL processes
    /// - Analytics on small tables  
    /// - Operations where you truly need to examine every item
    /// 
    /// Consider using Query operations instead whenever possible, as they are much more efficient.
    /// </summary>
    /// <returns>An interface that provides scan functionality while maintaining access to all core operations.</returns>
    /// <example>
    /// <code>
    /// // Access scan operations through the scannable interface
    /// var scannableTable = table.AsScannable();
    /// var results = await scannableTable.Scan
    ///     .WithFilter("#status = :active")
    ///     .WithAttribute("#status", "status")
    ///     .WithValue(":active", "ACTIVE")
    ///     .ExecuteAsync();
    /// 
    /// // Still access regular operations
    /// var item = await scannableTable.Get
    ///     .WithKey("id", "123")
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    public IScannableDynamoDbTable AsScannable()
    {
        return new ScannableDynamoDbTable(this);
    }
}