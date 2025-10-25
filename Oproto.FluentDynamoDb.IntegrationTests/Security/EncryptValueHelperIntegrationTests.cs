using System.Text;
using Amazon.DynamoDBv2.Model;
using Oproto.FluentDynamoDb.IntegrationTests.Infrastructure;
using Oproto.FluentDynamoDb.IntegrationTests.TestEntities;
using Oproto.FluentDynamoDb.Storage;

namespace Oproto.FluentDynamoDb.IntegrationTests.Security;

/// <summary>
/// Integration tests for table.EncryptValue() helper method.
/// Tests end-to-end encryption with DynamoDB Local using pre-encrypted values.
/// </summary>
[Collection("DynamoDB Local")]
[Trait("Category", "Integration")]
[Trait("Feature", "Encryption")]
[Trait("Feature", "EncryptValue")]
public class EncryptValueHelperIntegrationTests : IntegrationTestBase
{
    private class TestTable : DynamoDbTableBase
    {
        public TestTable(Amazon.DynamoDBv2.IAmazonDynamoDB client, string tableName, IFieldEncryptor? encryptor = null)
            : base(client, tableName, logger: null, fieldEncryptor: encryptor)
        {
        }
    }

    public EncryptValueHelperIntegrationTests(DynamoDbLocalFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Query_WithPreEncryptedValueInLinqExpression_QueriesCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var entity = new SecureTestEntity
        {
            Id = "user-123",
            Name = "John Doe",
            SocialSecurityNumber = "123-45-6789",
            Email = "john@example.com"
        };

        // Manually encrypt and store the entity
        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "tenant-123" },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Pre-encrypt value and use in LINQ expression
        EncryptionContext.Current = "tenant-123";
        var targetSsn = "123-45-6789";
        var encryptedTargetSsn = table.EncryptValue(targetSsn, "ssn");
        
        // Use pre-encrypted value in scan with filter
        var scanRequest = new ScanRequest
        {
            TableName = TableName,
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.ScanAsync(scanRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        
        var retrievedItem = response.Items[0];
        retrievedItem["pk"].S.Should().Be("user-123");
        retrievedItem["name"].S.Should().Be("John Doe");
        
        // Verify the SSN is encrypted and can be decrypted
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "tenant-123" },
            CancellationToken.None);
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be("123-45-6789");
    }

    [Fact]
    public async Task Query_WithPreEncryptedValueInFormatString_QueriesCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var entity = new SecureTestEntity
        {
            Id = "user-456",
            Name = "Jane Smith",
            SocialSecurityNumber = "987-65-4321",
            Email = "jane@example.com"
        };

        // Manually encrypt and store the entity
        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "tenant-456" },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Pre-encrypt value and use in format string expression
        EncryptionContext.Current = "tenant-456";
        var targetSsn = "987-65-4321";
        var encryptedTargetSsn = table.EncryptValue(targetSsn, "ssn");
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-456" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        
        var retrievedItem = response.Items[0];
        retrievedItem["pk"].S.Should().Be("user-456");
        retrievedItem["name"].S.Should().Be("Jane Smith");
        
        // Verify the SSN is encrypted and can be decrypted
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "tenant-456" },
            CancellationToken.None);
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be("987-65-4321");
    }

    [Fact]
    public async Task Scan_WithPreEncryptedValue_ScansCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        // Create multiple entities with encrypted SSNs
        var entities = new[]
        {
            new SecureTestEntity
            {
                Id = "user-1",
                Name = "Alice",
                SocialSecurityNumber = "111-11-1111",
                Email = "alice@example.com"
            },
            new SecureTestEntity
            {
                Id = "user-2",
                Name = "Bob",
                SocialSecurityNumber = "222-22-2222",
                Email = "bob@example.com"
            },
            new SecureTestEntity
            {
                Id = "user-3",
                Name = "Charlie",
                SocialSecurityNumber = "333-33-3333",
                Email = "charlie@example.com"
            }
        };

        // Store entities with encrypted SSNs
        foreach (var entity in entities)
        {
            var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber!);
            var encryptedSsn = await encryptor.EncryptAsync(
                ssnBytes,
                "ssn",
                new FieldEncryptionContext { ContextId = "scan-test" },
                CancellationToken.None);

            var item = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue { S = entity.Id },
                ["name"] = new AttributeValue { S = entity.Name },
                ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
                ["email"] = new AttributeValue { S = entity.Email! }
            };

            await DynamoDb.PutItemAsync(TableName, item);
        }

        // Act - Pre-encrypt value and use in scan filter
        EncryptionContext.Current = "scan-test";
        var targetSsn = "222-22-2222";
        var encryptedTargetSsn = table.EncryptValue(targetSsn, "ssn");
        
        var scanRequest = new ScanRequest
        {
            TableName = TableName,
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.ScanAsync(scanRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        response.Items[0]["pk"].S.Should().Be("user-2");
        response.Items[0]["name"].S.Should().Be("Bob");
        
        // Verify the SSN is encrypted and can be decrypted
        var storedSsnBytes = response.Items[0]["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "scan-test" },
            CancellationToken.None);
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be("222-22-2222");
    }

    [Fact]
    public async Task Query_WithPreEncryptedValueAndMultipleFields_EncryptsAllFieldsCorrectly()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var entity = new SecureTestEntity
        {
            Id = "user-multi",
            Name = "Multi Field User",
            SocialSecurityNumber = "555-55-5555",
            CreditCardNumber = "4111-1111-1111-1111",
            Email = "multi@example.com"
        };

        // Store entity with encrypted fields
        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var ccBytes = Encoding.UTF8.GetBytes(entity.CreditCardNumber);
        
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "multi-field" },
            CancellationToken.None);
        
        var encryptedCc = await encryptor.EncryptAsync(
            ccBytes,
            "credit_card",
            new FieldEncryptionContext { ContextId = "multi-field" },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["credit_card"] = new AttributeValue { B = new MemoryStream(encryptedCc) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Pre-encrypt multiple values and use in query
        EncryptionContext.Current = "multi-field";
        var targetSsn = "555-55-5555";
        var targetCc = "4111-1111-1111-1111";
        var encryptedTargetSsn = table.EncryptValue(targetSsn, "ssn");
        var encryptedTargetCc = table.EncryptValue(targetCc, "credit_card");
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn AND credit_card = :cc",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-multi" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) },
                [":cc"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetCc)) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        var retrievedItem = response.Items[0];
        
        // Verify SSN is encrypted and can be decrypted
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "multi-field" },
            CancellationToken.None);
        Encoding.UTF8.GetString(decryptedSsnBytes).Should().Be("555-55-5555");
        
        // Verify credit card is encrypted and can be decrypted
        var storedCcBytes = retrievedItem["credit_card"].B.ToArray();
        var decryptedCcBytes = await encryptor.DecryptAsync(
            storedCcBytes,
            "credit_card",
            new FieldEncryptionContext { ContextId = "multi-field" },
            CancellationToken.None);
        Encoding.UTF8.GetString(decryptedCcBytes).Should().Be("4111-1111-1111-1111");
    }

    [Fact]
    public async Task Query_WithPreEncryptedValueAndDifferentContexts_UsesCorrectContext()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = MockFieldEncryptor.CreateWithContextKeys("context-A", "context-B");
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        // Store entity with context-A
        var entity = new SecureTestEntity
        {
            Id = "user-context",
            Name = "Context User",
            SocialSecurityNumber = "777-77-7777",
            Email = "context@example.com"
        };

        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "context-A" },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Pre-encrypt with correct context
        EncryptionContext.Current = "context-A";
        var targetSsn = "777-77-7777";
        var encryptedTargetSsn = table.EncryptValue(targetSsn, "ssn");
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-context" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        var retrievedItem = response.Items[0];
        
        // Verify decryption with correct context works
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "context-A" },
            CancellationToken.None);
        Encoding.UTF8.GetString(decryptedSsnBytes).Should().Be("777-77-7777");
        
        // Verify decryption with wrong context fails
        var decryptWithWrongContext = async () => await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "context-B" },
            CancellationToken.None);
        
        await decryptWithWrongContext.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Encryption context mismatch*");
    }

    [Fact]
    public async Task Query_WithPreEncryptedValueRoundTrip_MaintainsDataIntegrity()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var originalSsn = "999-99-9999";
        EncryptionContext.Current = "roundtrip";
        
        // Pre-encrypt the value
        var encryptedSsn = table.EncryptValue(originalSsn, "ssn");
        
        // Store entity with encrypted SSN
        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = "user-roundtrip" },
            ["name"] = new AttributeValue { S = "Roundtrip User" },
            ["ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedSsn)) }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Retrieve and decrypt
        var getRequest = new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue { S = "user-roundtrip" }
            }
        };

        var getResponse = await DynamoDb.GetItemAsync(getRequest);

        // Assert
        getResponse.IsItemSet.Should().BeTrue();
        var storedSsnBytes = getResponse.Item["ssn"].B.ToArray();
        
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = "roundtrip" },
            CancellationToken.None);
        
        var decryptedSsn = Encoding.UTF8.GetString(decryptedSsnBytes);
        decryptedSsn.Should().Be(originalSsn);
        
        // Verify the encrypted value is base64-encoded
        var decoded = Convert.FromBase64String(encryptedSsn);
        decoded.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Query_WithPreEncryptedValueReuse_EncryptsOnce()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        // Create multiple entities with the same SSN
        var entities = new[]
        {
            new { Id = "user-A", Ssn = "111-11-1111", Name = "Alice" },
            new { Id = "user-B", Ssn = "111-11-1111", Name = "Bob" },
            new { Id = "user-C", Ssn = "111-11-1111", Name = "Charlie" }
        };

        foreach (var entity in entities)
        {
            var ssnBytes = Encoding.UTF8.GetBytes(entity.Ssn);
            var encryptedSsn = await encryptor.EncryptAsync(
                ssnBytes,
                "ssn",
                new FieldEncryptionContext { ContextId = "reuse-test" },
                CancellationToken.None);

            var item = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue { S = entity.Id },
                ["name"] = new AttributeValue { S = entity.Name },
                ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) }
            };

            await DynamoDb.PutItemAsync(TableName, item);
        }

        // Act - Pre-encrypt once and reuse in multiple queries
        EncryptionContext.Current = "reuse-test";
        var targetSsn = "111-11-1111";
        var encryptedTargetSsn = table.EncryptValue(targetSsn, "ssn");
        
        // Use the same encrypted value in scan
        var scanRequest = new ScanRequest
        {
            TableName = TableName,
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.ScanAsync(scanRequest);

        // Assert - Should find all three entities with the same SSN
        response.Items.Should().HaveCount(3);
        response.Items.Should().Contain(i => i["pk"].S == "user-A");
        response.Items.Should().Contain(i => i["pk"].S == "user-B");
        response.Items.Should().Contain(i => i["pk"].S == "user-C");
    }

    [Fact]
    public async Task Query_WithNullEncryptionContext_EncryptsWithNullContext()
    {
        // Arrange
        await CreateTableAsync<SecureTestEntity>();
        
        var encryptor = new MockFieldEncryptor();
        var table = new TestTable(DynamoDb, TableName, encryptor);
        
        var entity = new SecureTestEntity
        {
            Id = "user-null",
            Name = "Null Context User",
            SocialSecurityNumber = "000-00-0000",
            Email = "null@example.com"
        };

        // Store with null context
        var ssnBytes = Encoding.UTF8.GetBytes(entity.SocialSecurityNumber);
        var encryptedSsn = await encryptor.EncryptAsync(
            ssnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = null },
            CancellationToken.None);

        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = entity.Id },
            ["name"] = new AttributeValue { S = entity.Name },
            ["ssn"] = new AttributeValue { B = new MemoryStream(encryptedSsn) },
            ["email"] = new AttributeValue { S = entity.Email }
        };

        await DynamoDb.PutItemAsync(TableName, item);

        // Act - Pre-encrypt with null context
        EncryptionContext.Current = null;
        var targetSsn = "000-00-0000";
        var encryptedTargetSsn = table.EncryptValue(targetSsn, "ssn");
        
        var queryRequest = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            FilterExpression = "ssn = :ssn",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "user-null" },
                [":ssn"] = new AttributeValue { B = new MemoryStream(Convert.FromBase64String(encryptedTargetSsn)) }
            }
        };

        var response = await DynamoDb.QueryAsync(queryRequest);

        // Assert
        response.Items.Should().HaveCount(1);
        var retrievedItem = response.Items[0];
        
        // Verify decryption with null context works
        var storedSsnBytes = retrievedItem["ssn"].B.ToArray();
        var decryptedSsnBytes = await encryptor.DecryptAsync(
            storedSsnBytes,
            "ssn",
            new FieldEncryptionContext { ContextId = null },
            CancellationToken.None);
        Encoding.UTF8.GetString(decryptedSsnBytes).Should().Be("000-00-0000");
    }

    [Fact]
    public void EncryptValue_WithNoEncryptor_ThrowsInvalidOperationException()
    {
        // Arrange
        var table = new TestTable(DynamoDb, TableName, encryptor: null);
        EncryptionContext.Current = "tenant-123";

        // Act & Assert
        var act = () => table.EncryptValue("123-45-6789", "ssn");
        
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IFieldEncryptor not configured*")
            .WithMessage("*Pass an IFieldEncryptor instance to the table constructor*");
    }
}
