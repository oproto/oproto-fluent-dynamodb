# Requirements Document

## Introduction

This specification addresses the remaining 170 build errors in the integration test project after completing the initial integration-test-build-fixes tasks. The errors fall into several categories: missing generated table accessors, missing entity types, missing API methods, and test infrastructure issues.

## Glossary

- **Integration Test Project**: The Oproto.FluentDynamoDb.IntegrationTests project containing tests that verify library functionality against DynamoDB Local
- **Source Generator**: The Roslyn source generator that creates table classes, entity accessors, and mapping code
- **Table Accessor**: Generated properties on table classes that provide typed access to entities (e.g., `table.Orders`, `table.Users`)
- **Test Entity**: Entity classes used in integration tests, decorated with DynamoDB attributes
- **Request Builder**: Fluent API classes for building DynamoDB operation requests
- **Operation Context**: The DynamoDbOperationContext that tracks metadata about DynamoDB operations

## Requirements

### Requirement 1

**User Story:** As a developer, I want all integration tests to compile successfully, so that I can run the test suite to verify library functionality

#### Acceptance Criteria

1. WHEN THE Integration Test Project is built, THE Build System SHALL complete without compilation errors
2. WHEN missing table accessors are identified, THE Source Generator SHALL generate the required accessor properties for all test entities
3. WHEN missing entity types are referenced in tests, THE Test Project SHALL define all required entity classes with appropriate attributes
4. WHEN missing API methods are called in tests, THE Request Builder Classes SHALL provide all required fluent API methods
5. WHERE test infrastructure issues exist, THE Test Infrastructure SHALL be updated to support all test scenarios

### Requirement 2

**User Story:** As a developer, I want the source generator to create all necessary table accessors, so that tests can access entities through strongly-typed properties

#### Acceptance Criteria

1. WHEN a table class is defined with multiple entity types, THE Source Generator SHALL generate accessor properties for each entity type
2. WHEN an entity is marked with `[DynamoDbEntity]`, THE Source Generator SHALL create a corresponding table accessor property
3. WHEN table accessors are generated, THE Generated Code SHALL follow naming conventions (e.g., entity `OrderEntity` creates accessor `Orders`)
4. WHERE entity types share a table, THE Source Generator SHALL generate accessors for all entity types in that table
5. WHEN accessor generation fails, THE Source Generator SHALL emit diagnostic messages indicating the issue

### Requirement 3

**User Story:** As a developer, I want all test entity types to be properly defined, so that tests can instantiate and manipulate test data

#### Acceptance Criteria

1. WHEN a test references an undefined entity type, THE Test Project SHALL define the entity class with required properties
2. WHEN entity types are defined, THE Entity Classes SHALL include all properties referenced in tests
3. WHEN entities are used in multi-entity tables, THE Entity Classes SHALL include appropriate discriminator attributes
4. WHERE entities have relationships, THE Entity Classes SHALL define relationship properties with correct attributes
5. WHEN entity definitions are incomplete, THE Build System SHALL report clear error messages

### Requirement 4

**User Story:** As a developer, I want request builders to provide all necessary API methods, so that tests can configure DynamoDB operations correctly

#### Acceptance Criteria

1. WHEN tests call `ReturnValues()` on request builders, THE Request Builder Classes SHALL provide this method
2. WHEN tests call `ToDynamoDbResponseAsync()` on transaction builders, THE Transaction Builder Classes SHALL provide this method
3. WHEN API methods are added, THE Method Signatures SHALL match the usage patterns in tests
4. WHERE methods return builders for chaining, THE Return Types SHALL support fluent API patterns
5. WHEN API methods are missing, THE Compiler SHALL report clear error messages with method names

### Requirement 5

**User Story:** As a developer, I want test infrastructure to support all test scenarios, so that integration tests can verify complex library features

#### Acceptance Criteria

1. WHEN tests use operation context features, THE Test Infrastructure SHALL provide necessary context setup methods
2. WHEN tests verify generated code, THE Test Infrastructure SHALL provide access to generated table classes
3. WHEN tests need mock dependencies, THE Test Infrastructure SHALL provide appropriate mock implementations
4. WHERE tests require specific DynamoDB configurations, THE Test Infrastructure SHALL support configuration options
5. WHEN infrastructure is insufficient, THE Test Failures SHALL clearly indicate missing infrastructure components
