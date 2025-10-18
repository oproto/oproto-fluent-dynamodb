using TestMultiTargeting;

Console.WriteLine("Testing multi-targeting and source generator...");

var entity = new TestEntity
{
    Id = "test-123",
    Name = "Test User",
    Age = 25
};

Console.WriteLine($"Entity: {entity.Id}, {entity.Name}, {entity.Age}");

// Test that the source generator created the Fields class
Console.WriteLine($"Field constants: {TestEntityFields.Id}, {TestEntityFields.Name}, {TestEntityFields.Age}");

// Test that the source generator created the Keys class  
Console.WriteLine($"Partition key: {TestEntityKeys.Pk(entity.Id)}");

Console.WriteLine("Multi-targeting and source generator test completed successfully!");