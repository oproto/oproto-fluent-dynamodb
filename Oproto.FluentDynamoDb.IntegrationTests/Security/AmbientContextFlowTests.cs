using Oproto.FluentDynamoDb.IntegrationTests.Infrastructure;
using Oproto.FluentDynamoDb.Storage;

namespace Oproto.FluentDynamoDb.IntegrationTests.Security;

/// <summary>
/// Integration tests for ambient encryption context flow using AsyncLocal.
/// Validates thread-safety and context isolation between async operations.
/// </summary>
public class AmbientContextFlowTests : IntegrationTestBase
{
    public AmbientContextFlowTests(DynamoDbLocalFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AmbientContext_FlowsThroughAsyncCalls()
    {
        // Arrange
        var contextId = "tenant-123";

        // Act - Set ambient context
        EncryptionContext.Current = contextId;

        // Simulate async operations
        await Task.Delay(10);
        var retrievedContext1 = EncryptionContext.Current;

        await Task.Delay(10);
        var retrievedContext2 = EncryptionContext.Current;

        // Assert - Context flows through async calls
        retrievedContext1.Should().Be(contextId);
        retrievedContext2.Should().Be(contextId);

        // Cleanup
        EncryptionContext.Current = null;
    }

    [Fact]
    public async Task AmbientContext_IsolatesBetweenAsyncFlows()
    {
        // Arrange & Act - Start two concurrent async operations with different contexts
        var task1 = Task.Run(async () =>
        {
            EncryptionContext.Current = "tenant-a";
            await Task.Delay(50);
            return EncryptionContext.Current;
        });

        var task2 = Task.Run(async () =>
        {
            EncryptionContext.Current = "tenant-b";
            await Task.Delay(50);
            return EncryptionContext.Current;
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert - Each async flow maintains its own context
        results[0].Should().Be("tenant-a", "first async flow should maintain its context");
        results[1].Should().Be("tenant-b", "second async flow should maintain its context");
    }


    [Fact]
    public async Task AmbientContext_DoesNotLeakAcrossThreads()
    {
        // Arrange
        EncryptionContext.Current = "main-thread-context";

        // Act - Start a new task on a different thread
        var contextInNewThread = await Task.Run(() =>
        {
            // New thread should not see the main thread's context
            return EncryptionContext.Current;
        });

        // Assert
        contextInNewThread.Should().BeNull("context should not leak to new threads");
        EncryptionContext.Current.Should().Be("main-thread-context", "main thread context should be preserved");

        // Cleanup
        EncryptionContext.Current = null;
    }

    [Fact]
    public async Task AmbientContext_CanBeCleared()
    {
        // Arrange
        EncryptionContext.Current = "tenant-123";
        EncryptionContext.Current.Should().Be("tenant-123");

        // Act - Clear context
        EncryptionContext.Current = null;

        // Assert
        EncryptionContext.Current.Should().BeNull();

        // Verify it stays null through async operations
        await Task.Delay(10);
        EncryptionContext.Current.Should().BeNull();
    }

    [Fact]
    public async Task AmbientContext_SupportsNestedAsyncOperations()
    {
        // Arrange
        EncryptionContext.Current = "outer-context";

        // Act - Nested async operations
        var outerContext = EncryptionContext.Current;
        
        await Task.Run(async () =>
        {
            // Inner operation sees outer context
            var innerContext1 = EncryptionContext.Current;
            innerContext1.Should().Be("outer-context");

            // Change context in inner operation
            EncryptionContext.Current = "inner-context";
            await Task.Delay(10);
            
            var innerContext2 = EncryptionContext.Current;
            innerContext2.Should().Be("inner-context");
        });

        // Assert - Outer context is preserved
        EncryptionContext.Current.Should().Be("outer-context", 
            "outer context should not be affected by inner operation");

        // Cleanup
        EncryptionContext.Current = null;
    }

    [Fact]
    public async Task AmbientContext_WorksWithParallelOperations()
    {
        // Arrange & Act - Multiple parallel operations with different contexts
        var tasks = Enumerable.Range(1, 10).Select(i => Task.Run(async () =>
        {
            var contextId = $"tenant-{i}";
            EncryptionContext.Current = contextId;
            
            // Simulate some work
            await Task.Delay(Random.Shared.Next(10, 50));
            
            // Verify context is still correct
            return (Expected: contextId, Actual: EncryptionContext.Current);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert - All operations maintained their own context
        foreach (var (expected, actual) in results)
        {
            actual.Should().Be(expected, "each parallel operation should maintain its own context");
        }
    }
}
