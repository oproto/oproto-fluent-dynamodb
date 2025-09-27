# oproto-fluent-dynamodb
Oproto.FluentDynamoDb library

A fluent-style API wrapper for Amazon DynamoDB that provides a type-safe, intuitive interface for all DynamoDB operations. This implementation is safe for use in AOT (Ahead-of-Time) compilation projects and follows fluent design patterns for enhanced developer experience.

## Table and Index Definition
An optional feature of FluentDynamoDb it so define your tables, indexes and access patterns using the DynamoDbTableBase class.

```csharp
public class ToDoTable(IAmazonDynamoDB client, string todoTableName) : DynamoDbTableBase(client,todoTableName)
{
    Gsi1 = new DynamoDbIndex(this,"gsi1");
    
    public DynamoDbIndex Gsi1 { get; private init; }
}
```

You can then access all operation builders using the Get, Put, Update, Query, and Delete properties.
```csharp
var table = new ToDoTable(...);
var getItemResponse = await table.Get.WithKey("pk", todoId).ExecuteAsync();
var putItemResponse = await table.Put.WithItem(item).ExecuteAsync();
var updateItemResponse = await table.Update.WithKey("pk", todoId).Set("SET #status = :status").WithValue(":status", "completed").ExecuteAsync();
var deleteItemResponse = await table.Delete.WithKey("pk", todoId).ExecuteAsync();
```

You can further customize your table class implementation to include access patterns.
```csharp
public class ToDoTable(IAmazonDynamoDB client, string todoTableName) : DynamoDbTableBase(client,todoTableName)
{
    Gsi1 = new DynamoDbIndex(this,"gsi1");
    
    public DynamoDbIndex Gsi1 { get; private init; }
    
    public async Task<GetItemResponse> GetTodoAsync(string todoId) =>
        await Get.WithKey("pk", todoId).ExecuteAsync();
}
```

You can then access your access pattern as follows.
```csharp
var table = new ToDoTable(...);
var getItemResponse = await table.GetTodoAsync(todoId);
```

## Basic Operations

### Get Item
Retrieve a single item by its primary key:
```csharp
// Simple key
var response = await table.Get
    .WithKey("pk", "user123")
    .ExecuteAsync();

// Composite key
var response = await table.Get
    .WithKey("pk", "user123", "sk", "profile")
    .WithProjection("username, email, #status")
    .WithAttributeName("#status", "status")
    .UsingConsistentRead()
    .ExecuteAsync();
```

### Put Item
Add or replace an item:
```csharp
var item = new Dictionary<string, AttributeValue>
{
    ["pk"] = new AttributeValue("user123"),
    ["username"] = new AttributeValue("john_doe"),
    ["email"] = new AttributeValue("john@example.com")
};

var response = await table.Put
    .WithItem(item)
    .Where("attribute_not_exists(pk)") // Conditional put
    .ExecuteAsync();
```

### Update Item
Modify an existing item:
```csharp
var response = await table.Update
    .WithKey("pk", "user123")
    .Set("SET #status = :status, lastModified = :timestamp")
    .WithAttributeName("#status", "status")
    .WithValue(":status", "active")
    .WithValue(":timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
    .Where("attribute_exists(pk)")
    .ReturnAllNewValues()
    .ExecuteAsync();
```

### Delete Item
Remove an item from the table:
```csharp
// Simple delete
var response = await table.Delete
    .WithKey("pk", "user123")
    .ExecuteAsync();

// Conditional delete with return values
var response = await table.Delete
    .WithKey("pk", "user123", "sk", "profile")
    .Where("#status = :status")
    .WithAttributeName("#status", "status")
    .WithValue(":status", "inactive")
    .ReturnAllOldValues()
    .ExecuteAsync();

// Delete with condition check failure handling
try
{
    var response = await table.Delete
        .WithKey("pk", "user123")
        .Where("attribute_exists(pk) AND #version = :expectedVersion")
        .WithAttributeName("#version", "version")
        .WithValue(":expectedVersion", 5)
        .ExecuteAsync();
}
catch (ConditionalCheckFailedException)
{
    // Handle optimistic locking failure
}
```

### Query Operations
Query items using partition key and optional sort key conditions:
```csharp
// Basic query
var response = await table.Query
    .Where("pk = :pk")
    .WithValue(":pk", "user123")
    .ExecuteAsync();

// Advanced query with filtering and projection
var response = await table.Query
    .Where("pk = :pk AND begins_with(sk, :skPrefix)")
    .WithValue(":pk", "user123")
    .WithValue(":skPrefix", "order#")
    .WithFilter("#status = :status")
    .WithAttributeName("#status", "status")
    .WithValue(":status", "completed")
    .WithProjection("orderId, amount, #status")
    .ScanIndexForward(false) // Descending order
    .Take(20)
    .ExecuteAsync();

// Query with pagination
var response = await table.Query
    .Where("pk = :pk")
    .WithValue(":pk", "user123")
    .StartAt(lastEvaluatedKey) // From previous page
    .Take(10)
    .ExecuteAsync();
```

### Scan Operations (Use with Caution)
Scan operations are intentionally made less accessible to prevent accidental misuse. Access them through the `AsScannable()` method:

```csharp
// Basic scan - note the intentional friction
var scannableTable = table.AsScannable();
var response = await scannableTable.Scan
    .WithFilter("#status = :status")
    .WithAttributeName("#status", "status")
    .WithValue(":status", "active")
    .WithProjection("pk, username, email")
    .Take(100)
    .ExecuteAsync();

// Parallel scan for large datasets
var segment1Task = scannableTable.Scan
    .WithSegment(0, 4) // Segment 0 of 4 total segments
    .WithFilter("#status = :status")
    .WithAttributeName("#status", "status")
    .WithValue(":status", "active")
    .ExecuteAsync();

var segment2Task = scannableTable.Scan
    .WithSegment(1, 4) // Segment 1 of 4 total segments
    .WithFilter("#status = :status")
    .WithAttributeName("#status", "status")
    .WithValue(":status", "active")
    .ExecuteAsync();

// Process segments in parallel
var results = await Task.WhenAll(segment1Task, segment2Task);

// Count scan
var countResponse = await scannableTable.Scan
    .WithFilter("#status = :status")
    .WithAttributeName("#status", "status")
    .WithValue(":status", "active")
    .Count()
    .ExecuteAsync();

// Access underlying table if needed
var originalTable = scannableTable.UnderlyingTable;
```

## Batch Operations

### Batch Get Items
Retrieve multiple items in a single request for better performance:

```csharp
// Single table batch get
var response = await new BatchGetItemRequestBuilder(dynamoDbClient)
    .GetFromTable("Users", builder => builder
        .WithKey("pk", "user1")
        .WithKey("pk", "user2")
        .WithKey("pk", "user3")
        .WithProjection("pk, username, email")
        .UsingConsistentRead())
    .ExecuteAsync();

// Multiple tables batch get
var response = await new BatchGetItemRequestBuilder(dynamoDbClient)
    .GetFromTable("Users", builder => builder
        .WithKey("pk", "user1")
        .WithKey("pk", "user2")
        .WithProjection("pk, username, email"))
    .GetFromTable("Orders", builder => builder
        .WithKey("pk", "user1", "sk", "order#123")
        .WithKey("pk", "user2", "sk", "order#456")
        .UsingConsistentRead())
    .ReturnConsumedCapacity()
    .ExecuteAsync();

// Handle unprocessed keys
var response = await new BatchGetItemRequestBuilder(dynamoDbClient)
    .GetFromTable("Users", builder => builder
        .WithKey("pk", "user1")
        .WithKey("pk", "user2"))
    .ExecuteAsync();

if (response.UnprocessedKeys.Count > 0)
{
    // Retry unprocessed keys or handle them separately
    foreach (var table in response.UnprocessedKeys)
    {
        Console.WriteLine($"Unprocessed keys for table {table.Key}: {table.Value.Keys.Count}");
    }
}
```

### Batch Write Items
Perform multiple put and delete operations in a single request:

```csharp
// Single table batch write with mixed operations
var response = await new BatchWriteItemRequestBuilder(dynamoDbClient)
    .WriteToTable("Users", builder => builder
        .PutItem(new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue("user1"),
            ["username"] = new AttributeValue("john_doe"),
            ["email"] = new AttributeValue("john@example.com")
        })
        .PutItem(new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue("user2"),
            ["username"] = new AttributeValue("jane_doe"),
            ["email"] = new AttributeValue("jane@example.com")
        })
        .DeleteItem("pk", "user3")) // Delete by key
    .ExecuteAsync();

// Multiple tables batch write
var response = await new BatchWriteItemRequestBuilder(dynamoDbClient)
    .WriteToTable("Users", builder => builder
        .PutItem(new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue("user1"),
            ["username"] = new AttributeValue("updated_user")
        })
        .DeleteItem("pk", "user2"))
    .WriteToTable("UserProfiles", builder => builder
        .PutItem(new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue("user1"),
            ["sk"] = new AttributeValue("profile"),
            ["bio"] = new AttributeValue("Updated bio")
        })
        .DeleteItem("pk", "user2", "sk", "profile")) // Composite key delete
    .ReturnConsumedCapacity()
    .ReturnItemCollectionMetrics()
    .ExecuteAsync();

// Using model mapping for put operations
public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}

var users = new List<User>
{
    new() { Id = "user1", Username = "john", Email = "john@example.com" },
    new() { Id = "user2", Username = "jane", Email = "jane@example.com" }
};

var response = await new BatchWriteItemRequestBuilder(dynamoDbClient)
    .WriteToTable("Users", builder =>
    {
        foreach (var user in users)
        {
            builder.PutItem(user, u => new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue(u.Id),
                ["username"] = new AttributeValue(u.Username),
                ["email"] = new AttributeValue(u.Email)
            });
        }
    })
    .ExecuteAsync();

// Handle unprocessed items
if (response.UnprocessedItems.Count > 0)
{
    // Retry unprocessed items or handle them separately
    foreach (var table in response.UnprocessedItems)
    {
        Console.WriteLine($"Unprocessed items for table {table.Key}: {table.Value.Count}");
    }
}
```

## Advanced Features

### Pagination
Pagination features in FluentDynamoDb are optional and work with both Query and Scan operations.

The Pagination extension method takes an implementation of IPaginationRequest.
If your service's request models implement this interface, you can pass the request object directly.

```csharp
// Using pagination with Query
var queryResponse = await table.Gsi1.Query
    .Where("gsi1pk = :gsi1pk")
    .WithValue(":gsi1pk", "foo")
    .Paginate(paginationRequest)
    .ExecuteAsync();

// Using pagination with Scan
var scanResponse = await table.AsScannable().Scan
    .WithFilter("#status = :status")
    .WithAttributeName("#status", "status")
    .WithValue(":status", "active")
    .Paginate(paginationRequest)
    .ExecuteAsync();

// Manual pagination
var firstPage = await table.Query
    .Where("pk = :pk")
    .WithValue(":pk", "user123")
    .Take(10)
    .ExecuteAsync();

if (firstPage.LastEvaluatedKey != null)
{
    var secondPage = await table.Query
        .Where("pk = :pk")
        .WithValue(":pk", "user123")
        .StartAt(firstPage.LastEvaluatedKey)
        .Take(10)
        .ExecuteAsync();
}
```

If you need to call your page size and request token values something different, the PaginationRequest class provides a default implementation you can pass in.

```csharp
var paginationRequest = new PaginationRequest
{
    PageSize = 25,
    RequestToken = "eyJwayI6InVzZXIxMjMifQ==" // Base64 encoded last evaluated key
};

var response = await table.Query
    .Where("pk = :pk")
    .WithValue(":pk", "user123")
    .Paginate(paginationRequest)
    .ExecuteAsync();
```

### Transactions
Transactions work slightly different since they aren't tied to a single table. They start from a TransactWriteItemsRequestBuilder or TransactGetItemsRequestBuilder.

#### Write Transactions
```csharp
// Complex write transaction with multiple operations
var transactionResult = await new TransactWriteItemsRequestBuilder(dynamoDbClient)
    .WithClientRequestToken("unique-token-1234")
    .CheckCondition(userTable, condition =>
        condition.WithKey("pk", "user123")
                 .Where("attribute_exists(pk) AND #status = :status")
                 .WithAttributeName("#status", "status")
                 .WithValue(":status", "active"))
    .Update(userTable, upd =>
        upd.WithKey("pk", "user123")
           .Set("SET balance = balance - :amount, lastTransaction = :timestamp")
           .WithValue(":amount", 100)
           .WithValue(":timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
           .Where("balance >= :amount"))
    .Put(transactionTable, put =>
        put.WithItem(new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue("txn123"),
            ["userId"] = new AttributeValue("user123"),
            ["amount"] = new AttributeValue { N = "100" },
            ["type"] = new AttributeValue("debit")
        })
        .Where("attribute_not_exists(pk)"))
    .Delete(tempTable, del =>
        del.WithKey("pk", "temp123")
           .Where("attribute_exists(pk)"))
    .ReturnConsumedCapacity()
    .ExecuteAsync();

// Simple transaction example
var result = await new TransactWriteItemsRequestBuilder(dynamoDbClient)
    .Update(table, upd =>
        upd.WithKey("pk", "item1")
           .Set("SET #count = #count + :inc")
           .WithAttributeName("#count", "count")
           .WithValue(":inc", 1))
    .Put(logTable, put =>
        put.WithItem(new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue("log123"),
            ["action"] = new AttributeValue("increment"),
            ["timestamp"] = new AttributeValue { N = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
        }))
    .ExecuteAsync();
```

#### Read Transactions
```csharp
// Read transaction for consistent reads across multiple items/tables
var readResult = await new TransactGetItemsRequestBuilder(dynamoDbClient)
    .Get(userTable, get =>
        get.WithKey("pk", "user123")
           .WithProjection("pk, username, balance"))
    .Get(accountTable, get =>
        get.WithKey("pk", "account456")
           .WithProjection("pk, accountNumber, balance"))
    .Get(userTable, get =>
        get.WithKey("pk", "user789")
           .WithProjection("pk, username, balance"))
    .ReturnConsumedCapacity()
    .ExecuteAsync();

// Process transaction results
foreach (var response in readResult.Responses)
{
    if (response.Item != null)
    {
        // Process each item
        var pk = response.Item["pk"].S;
        Console.WriteLine($"Retrieved item: {pk}");
    }
}
```

### Stream Processing
FluentDynamoDb can help handle processing DynamoDb Stream events in Amazon Lambda functions.

```csharp
// Basic stream processing
foreach (var record in streamEvent.Records)
{
    await record.Process()
        .OnPatternMatch("pk", new Regex(@"^user#[0-9]+$"), "sk", new Regex(@"^profile$"),
            (processor) => processor
                .OnInsert(async (r) => await HandleUserProfileCreated(r))
                .OnUpdate(async (r) => await HandleUserProfileUpdated(r))
                .OnDelete(async (r) => await HandleUserProfileDeleted(r))
        )
        .OnPatternMatch("pk", new Regex(@"^user#[0-9]+$"), "sk", new Regex(@"^order#"),
            (processor) => processor
                .OnInsert(async (r) => await HandleOrderCreated(r))
                .OnUpdate(async (r) => await HandleOrderUpdated(r))
        );
}

// Advanced stream processing with specific event handling
foreach (var record in streamEvent.Records)
{
    await record.Process()
        .OnMatch("pk", "config", "sk", "settings",
            (processor) => processor
                .OnUpdate(async (r) => await RefreshApplicationCache(r))
        )
        .OnSortKeyMatch("sk", "inventory",
            (processor) => processor
                .OnUpdate(async (r) => await UpdateInventoryCache(r))
                .OnDelete(async (r) => await RemoveFromInventoryCache(r))
        )
        .OnPatternMatch("pk", new Regex(@"^session#"), 
            (processor) => processor
                .OnTtlDelete(async (r) => await CleanupExpiredSession(r))
                .OnNonTtlDelete(async (r) => await HandleManualSessionDeletion(r))
        );
}

// Helper methods for stream processing
private async Task HandleUserProfileCreated(DynamoDBStreamRecord record)
{
    var newImage = record.Dynamodb.NewImage;
    var userId = newImage["pk"].S.Replace("user#", "");
    
    // Send welcome email, update analytics, etc.
    await SendWelcomeEmail(userId);
    await UpdateUserAnalytics("user_created", userId);
}

private async Task HandleOrderCreated(DynamoDBStreamRecord record)
{
    var newImage = record.Dynamodb.NewImage;
    var orderId = newImage["sk"].S;
    var amount = decimal.Parse(newImage["amount"].N);
    
    // Process payment, update inventory, send notifications
    await ProcessPayment(orderId, amount);
    await UpdateInventory(newImage);
    await SendOrderConfirmation(orderId);
}
```

#### Available Key Matching Methods
- **OnMatch**: Exact key matching for both partition and sort keys
- **OnSortKeyMatch**: Match only the sort key (any partition key)
- **OnPatternMatch**: Regex pattern matching for both keys
- **OnSortKeyPatternMatch**: Regex pattern matching for sort key only

#### Available Event Type Filters
The DynamoDbRecordEventProcessor instance passed to your lambda expression has event-type filters:
- **OnInsert**: Handle new item creation
- **OnUpdate**: Handle item modifications
- **OnDelete**: Handle any item deletion
- **OnNonTtlDelete**: Handle manual item deletions (not TTL)
- **OnTtlDelete**: Handle TTL-based item deletions

#### Error Handling in Stream Processing
```csharp
foreach (var record in streamEvent.Records)
{
    try
    {
        await record.Process()
            .OnPatternMatch("pk", new Regex(@"^user#"), 
                (processor) => processor
                    .OnInsert(async (r) => 
                    {
                        try
                        {
                            await ProcessUserCreation(r);
                        }
                        catch (Exception ex)
                        {
                            // Log error but don't fail the entire batch
                            logger.LogError(ex, "Failed to process user creation for record {RecordId}", r.EventID);
                            await SendToDeadLetterQueue(r, ex);
                        }
                    })
            );
    }
    catch (Exception ex)
    {
        // Handle processing errors
        logger.LogError(ex, "Failed to process stream record {RecordId}", record.EventID);
        throw; // Re-throw to fail the batch if needed
    }
}
```

## Error Handling and Best Practices

### Common Exception Handling
```csharp
try
{
    var response = await table.Put
        .WithItem(item)
        .Where("attribute_not_exists(pk)")
        .ExecuteAsync();
}
catch (ConditionalCheckFailedException)
{
    // Item already exists
    throw new InvalidOperationException("Item already exists");
}
catch (ProvisionedThroughputExceededException)
{
    // Rate limiting - implement exponential backoff
    await Task.Delay(TimeSpan.FromMilliseconds(100));
    // Retry logic here
}
catch (ResourceNotFoundException)
{
    // Table doesn't exist
    throw new InvalidOperationException("Table not found");
}
catch (ValidationException ex)
{
    // Invalid request parameters
    throw new ArgumentException($"Invalid request: {ex.Message}");
}
```

### Optimistic Locking Pattern
```csharp
public async Task<bool> UpdateItemWithOptimisticLocking(string itemId, string newValue, int expectedVersion)
{
    try
    {
        await table.Update
            .WithKey("pk", itemId)
            .Set("SET #value = :newValue, #version = #version + :inc")
            .WithAttributeName("#value", "value")
            .WithAttributeName("#version", "version")
            .WithValue(":newValue", newValue)
            .WithValue(":inc", 1)
            .Where("#version = :expectedVersion")
            .WithValue(":expectedVersion", expectedVersion)
            .ExecuteAsync();
        
        return true;
    }
    catch (ConditionalCheckFailedException)
    {
        // Version mismatch - item was modified by another process
        return false;
    }
}
```

### Batch Operation Best Practices
```csharp
// Handle batch size limits (25 items max per batch)
public async Task BatchWriteItems<T>(IEnumerable<T> items, Func<T, Dictionary<string, AttributeValue>> mapper)
{
    var batches = items.Chunk(25); // Split into batches of 25
    
    foreach (var batch in batches)
    {
        var builder = new BatchWriteItemRequestBuilder(dynamoDbClient);
        
        builder.WriteToTable(tableName, tableBuilder =>
        {
            foreach (var item in batch)
            {
                tableBuilder.PutItem(item, mapper);
            }
        });
        
        var response = await builder.ExecuteAsync();
        
        // Handle unprocessed items with exponential backoff
        var unprocessedItems = response.UnprocessedItems;
        var retryAttempts = 0;
        
        while (unprocessedItems.Count > 0 && retryAttempts < 3)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempts) * 100));
            
            var retryBuilder = new BatchWriteItemRequestBuilder(dynamoDbClient);
            foreach (var table in unprocessedItems)
            {
                retryBuilder.WriteToTable(table.Key, tableBuilder =>
                {
                    foreach (var writeRequest in table.Value)
                    {
                        if (writeRequest.PutRequest != null)
                        {
                            tableBuilder.PutItem(writeRequest.PutRequest.Item);
                        }
                        else if (writeRequest.DeleteRequest != null)
                        {
                            tableBuilder.DeleteItem(writeRequest.DeleteRequest.Key);
                        }
                    }
                });
            }
            
            var retryResponse = await retryBuilder.ExecuteAsync();
            unprocessedItems = retryResponse.UnprocessedItems;
            retryAttempts++;
        }
    }
}
```

### Performance Optimization Tips

#### 1. Use Projection Expressions
```csharp
// Good - only retrieve needed attributes
var response = await table.Get
    .WithKey("pk", "user123")
    .WithProjection("username, email, #status")
    .WithAttributeName("#status", "status")
    .ExecuteAsync();

// Avoid - retrieves entire item
var response = await table.Get
    .WithKey("pk", "user123")
    .ExecuteAsync();
```

#### 2. Prefer Query over Scan
```csharp
// Good - efficient query using partition key
var response = await table.Query
    .Where("pk = :pk AND begins_with(sk, :prefix)")
    .WithValue(":pk", "user123")
    .WithValue(":prefix", "order#")
    .ExecuteAsync();

// Avoid - inefficient scan
var response = await table.AsScannable().Scan
    .WithFilter("begins_with(sk, :prefix)")
    .WithValue(":prefix", "order#")
    .ExecuteAsync();
```

#### 3. Use Batch Operations for Multiple Items
```csharp
// Good - batch get for multiple items
var response = await new BatchGetItemRequestBuilder(dynamoDbClient)
    .GetFromTable("Users", builder => builder
        .WithKey("pk", "user1")
        .WithKey("pk", "user2")
        .WithKey("pk", "user3"))
    .ExecuteAsync();

// Avoid - multiple individual gets
var tasks = new[] { "user1", "user2", "user3" }
    .Select(id => table.Get.WithKey("pk", id).ExecuteAsync());
var responses = await Task.WhenAll(tasks);
```

#### 4. Implement Proper Pagination
```csharp
// Good - proper pagination with reasonable page size
var allItems = new List<Dictionary<string, AttributeValue>>();
Dictionary<string, AttributeValue> lastEvaluatedKey = null;

do
{
    var query = table.Query
        .Where("pk = :pk")
        .WithValue(":pk", "user123")
        .Take(100); // Reasonable page size
    
    if (lastEvaluatedKey != null)
    {
        query = query.StartAt(lastEvaluatedKey);
    }
    
    var response = await query.ExecuteAsync();
    allItems.AddRange(response.Items);
    lastEvaluatedKey = response.LastEvaluatedKey;
    
} while (lastEvaluatedKey != null);
```