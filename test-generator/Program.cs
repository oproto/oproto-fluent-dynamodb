using TestGenerator;

Console.WriteLine("Testing source generator...");

// Test if generated code exists
try
{
    Console.WriteLine($"TestEntityFields.Id: {TestEntityFields.Id}");
    Console.WriteLine($"TestEntityFields.Data: {TestEntityFields.Data}");
    
    var entity = new TestEntity { Id = "test-id", Data = "test-data" };
    var dict = TestEntity.ToDynamoDb(entity);
    
    Console.WriteLine($"Generated {dict.Count} attributes");
    Console.WriteLine("Source generator is working!");
}
catch (Exception ex)
{
    Console.WriteLine($"Source generator not working: {ex.Message}");
}