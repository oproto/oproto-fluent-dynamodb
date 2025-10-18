# Oproto.FluentDynamoDb.FluentResults

FluentResults extensions for Oproto.FluentDynamoDb providing `Result<T>` return patterns instead of exceptions.

## Installation

```bash
dotnet add package Oproto.FluentDynamoDb.FluentResults
```

## Usage

This package provides extension methods that wrap the enhanced ExecuteAsync methods to return `Result<T>` instead of throwing exceptions:

```csharp
using Oproto.FluentDynamoDb.FluentResults;

// Instead of try/catch blocks
var result = await table.Get
    .WithKey("id", "123")
    .ExecuteAsyncResult<MyEntity>();

if (result.IsSuccess)
{
    var entity = result.Value.Item;
    // Handle success
}
else
{
    // Handle errors without exceptions
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error.Message);
    }
}
```

## Available Methods

- `ExecuteAsyncResult<T>()` - For GetItem, Query, and Scan operations
- `WithItemResult<T>()` - For configuring PutItem with entities
- `ExecuteAsyncResult<T>(entity)` - For executing PutItem operations
- `GetDynamoDbItemsResult<T>()` - For converting entities to DynamoDB items

## Requirements

- Requires `Oproto.FluentDynamoDb` package
- Entities must implement `IDynamoDbEntity` interface
- .NET 8.0 or later

## Optional Dependency

This package is completely optional. The main `Oproto.FluentDynamoDb` library works independently and this package only adds FluentResults support for those who prefer the Result pattern over exceptions.