using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BasicUsage.Models;
using Oproto.FluentDynamoDb.Storage;

Console.WriteLine("DynamoDB Source Generator Example");
Console.WriteLine("=================================");

// Create a sample transaction
var transaction = new Transaction
{
    TenantId = "tenant-123",
    TransactionId = "txn-456",
    Amount = 150.75m,
    Description = "Online purchase",
    Status = "completed",
    CreatedDate = DateTime.UtcNow,
    UpdatedDate = DateTime.UtcNow,
    Metadata = new Dictionary<string, string>
    {
        ["category"] = "retail",
        ["payment_method"] = "credit_card"
    }
};

Console.WriteLine("1. Testing Generated Field Constants:");
Console.WriteLine($"   Partition Key Field: {TransactionFields.TenantId}");
Console.WriteLine($"   Sort Key Field: {TransactionFields.TransactionId}");
Console.WriteLine($"   Amount Field: {TransactionFields.Amount}");
Console.WriteLine($"   GSI Status Field: {TransactionFields.StatusIndex.Status}");

Console.WriteLine("\n2. Testing Generated Key Builders:");
var pk = TransactionKeys.Pk(transaction.TenantId);
var sk = TransactionKeys.Sk(transaction.TransactionId);
var gsiPk = TransactionKeys.StatusIndex.Pk(transaction.Status);
var gsiSk = TransactionKeys.StatusIndex.Sk(transaction.CreatedDate);

Console.WriteLine($"   Generated PK: {pk}");
Console.WriteLine($"   Generated SK: {sk}");
Console.WriteLine($"   Generated GSI PK: {gsiPk}");
Console.WriteLine($"   Generated GSI SK: {gsiSk}");

Console.WriteLine("\n3. Testing Generated Entity Mapping:");
var attributeDict = Transaction.ToDynamoDb(transaction);
Console.WriteLine($"   Mapped to {attributeDict.Count} DynamoDB attributes:");
foreach (var kvp in attributeDict)
{
    Console.WriteLine($"     {kvp.Key}: {kvp.Value}");
}

Console.WriteLine("\n4. Testing Round-trip Mapping:");
var mappedBack = Transaction.FromDynamoDb<Transaction>(attributeDict);
Console.WriteLine($"   Original Amount: {transaction.Amount}");
Console.WriteLine($"   Mapped Back Amount: {mappedBack.Amount}");
Console.WriteLine($"   Original Description: {transaction.Description}");
Console.WriteLine($"   Mapped Back Description: {mappedBack.Description}");

Console.WriteLine("\n5. Testing Entity Metadata:");
var metadata = Transaction.GetEntityMetadata();
Console.WriteLine($"   Table Name: {metadata.TableName}");
Console.WriteLine($"   Properties Count: {metadata.Properties.Length}");
Console.WriteLine($"   Indexes Count: {metadata.Indexes.Length}");

Console.WriteLine("\n6. Testing Entity Identification:");
var partitionKey = Transaction.GetPartitionKey(attributeDict);
var matchesEntity = Transaction.MatchesEntity(attributeDict);
Console.WriteLine($"   Extracted Partition Key: {partitionKey}");
Console.WriteLine($"   Matches Entity: {matchesEntity}");

Console.WriteLine("\nSource generator integration test completed successfully!");
Console.WriteLine("The generated code provides:");
Console.WriteLine("- Type-safe field constants");
Console.WriteLine("- Automatic key builders");
Console.WriteLine("- Efficient entity mapping");
Console.WriteLine("- Metadata for future LINQ support");
Console.WriteLine("- AOT-compatible code generation");