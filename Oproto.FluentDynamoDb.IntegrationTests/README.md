# Oproto.FluentDynamoDb Integration Tests

This project contains integration tests that verify the FluentDynamoDb library works correctly with actual DynamoDB Local.

## Prerequisites

- .NET 8 SDK
- Java 17+ (for DynamoDB Local)

## Project Structure

```
Oproto.FluentDynamoDb.IntegrationTests/
├── Infrastructure/          # DynamoDB Local fixture and test base classes
├── AdvancedTypes/          # Tests for HashSet, List, Dictionary types
├── BasicTypes/             # Tests for basic types (string, int, decimal, etc.)
├── RealWorld/              # Tests for realistic scenarios and complex entities
└── TestEntities/           # Test entity definitions and builders
```

## Running Tests

### Run all integration tests
```bash
dotnet test Oproto.FluentDynamoDb.IntegrationTests
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~HashSetIntegrationTests"
```

### Run tests by category
```bash
dotnet test --filter "Category=Integration"
```

## Setup

The integration tests will automatically:
1. Check if DynamoDB Local is already running
2. Download DynamoDB Local if not present
3. Start DynamoDB Local before tests
4. Stop DynamoDB Local after tests complete

## Troubleshooting

### DynamoDB Local won't start

- Ensure Java is installed: `java -version`
- Check if port 8000 is available: `lsof -i :8000` (macOS/Linux) or `netstat -ano | findstr :8000` (Windows)
- View DynamoDB Local logs in test output

### Tests fail with "Table not found"

- Check table creation in test setup
- Verify table name matches between setup and test
- Ensure cleanup from previous test run completed

## Test Categories

Tests are organized by trait:
- `Category=Integration` - All integration tests
- `Category=AdvancedTypes` - Tests for advanced collection types
- `Category=BasicTypes` - Tests for basic types
- `Category=RealWorld` - Tests for realistic scenarios
