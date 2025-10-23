# Requirements Document

## Introduction

This feature refactors the Oproto.FluentDynamoDb library's builder instantiation pattern from property-based access to method-based access. Currently, builders are accessed as properties (e.g., `table.Query.Where()`), which creates challenges for future LINQ expression support and adds complexity when dealing with different table schemas (single vs composite keys). The refactoring will change the API to use method calls for builder instantiation (e.g., `table.Query().Where()`), enabling natural progression to LINQ-style expressions (e.g., `table.Query(x => x.Id == id)`) in future iterations.

This change also addresses the complexity of having different method signatures for Get operations depending on whether a table has a single partition key or a composite key (partition + sort key), and similar issues with index queries where the generic type system becomes unwieldy.

## Glossary

- **Builder**: A fluent interface class that constructs DynamoDB request objects (e.g., QueryRequestBuilder, GetItemRequestBuilder)
- **Table**: The DynamoDbTableBase class that provides access to DynamoDB operations
- **Index**: A DynamoDB Global Secondary Index (GSI) or Local Secondary Index (LSI) represented by DynamoDbIndex classes
- **Property-based access**: Current pattern where builders are accessed as properties (e.g., `table.Query`)
- **Method-based access**: New pattern where builders are instantiated via method calls (e.g., `table.Query()`)
- **LINQ expression**: Language Integrated Query expressions using lambda syntax (e.g., `x => x.Id == id`)
- **Composite key**: A DynamoDB key consisting of both partition key and sort key
- **Source generator**: Code generation tool that creates table classes from entity attributes

## Requirements

### Requirement 1

**User Story:** As a library consumer, I want to call Query as a method instead of a property, so that the API can naturally evolve to support LINQ expressions in the future.

#### Acceptance Criteria

1. WHEN calling table.Query() THEN the System SHALL return a QueryRequestBuilder instance
2. WHEN chaining methods after Query() THEN the fluent interface SHALL work identically to the current property-based approach
3. WHEN the library adds LINQ support in the future THEN table.Query(x => x.Id == id) SHALL be a natural extension of the method-based API
4. WHEN using the method-based API THEN the syntax SHALL feel consistent with LINQ patterns

### Requirement 2

**User Story:** As a library consumer, I want Get operations to accept key parameters directly in the method call, so that I don't need separate WithKey() calls.

#### Acceptance Criteria

1. WHEN a table has a single partition key THEN table.Get(partitionKeyValue) SHALL return a configured GetItemRequestBuilder
2. WHEN a table has a composite key THEN table.Get(partitionKeyValue, sortKeyValue) SHALL return a configured GetItemRequestBuilder
3. WHEN using source-generated tables THEN the Get method SHALL be generated with the correct signature based on the table schema (either single or composite, not both)
4. WHEN using manual table definitions THEN derived classes SHALL override Get() to provide appropriate overloads

### Requirement 3

**User Story:** As a library consumer, I want Update and Delete operations to accept key parameters directly, so that the API is consistent with Get operations.

#### Acceptance Criteria

1. WHEN calling table.Update(partitionKeyValue) THEN the System SHALL return an UpdateItemRequestBuilder configured with the key
2. WHEN calling table.Delete(partitionKeyValue) THEN the System SHALL return a DeleteItemRequestBuilder configured with the key
3. WHEN a table has a composite key THEN Update(pk, sk) and Delete(pk, sk) SHALL be available
4. WHEN using the parameterless overload THEN the builder SHALL be returned without pre-configured keys for manual configuration

### Requirement 4

**User Story:** As a library consumer, I want Query operations on indexes to accept key parameters directly, so that index queries are as convenient as table queries.

#### Acceptance Criteria

1. WHEN calling index.Query() THEN the System SHALL return a QueryRequestBuilder configured for that index
2. WHEN calling index.Query(partitionKeyValue) THEN the System SHALL return a QueryRequestBuilder with the key condition pre-configured
3. WHEN an index has a composite key THEN index.Query(pk, sk) SHALL be available with appropriate key condition operators
4. WHEN using the parameterless overload THEN the builder SHALL be returned for manual query configuration

### Requirement 5

**User Story:** As a library maintainer, I want to remove property-based builder access in favor of method-based access, so that the API can evolve to support LINQ expressions.

#### Acceptance Criteria

1. WHEN the refactoring is complete THEN property-based builder access SHALL no longer be available
2. WHEN attempting to use property-based access THEN the compiler SHALL emit errors indicating methods must be used
3. WHEN the API is documented THEN it SHALL explain the benefits of method-based access
4. WHEN examples are provided THEN they SHALL demonstrate the method-based patterns

### Requirement 6

**User Story:** As a library consumer, I want the method-based API to support the same fluent chaining patterns, so that my query logic remains familiar.

#### Acceptance Criteria

1. WHEN using method-based builders THEN all existing fluent methods SHALL be available
2. WHEN chaining methods THEN the order and behavior SHALL be identical to previous versions
3. WHEN using format string expressions THEN they SHALL integrate seamlessly with other fluent methods
4. WHEN all methods are chained THEN the final ExecuteAsync() call SHALL produce identical results to v1.x

### Requirement 7

**User Story:** As a developer using source-generated tables, I want the generator to create method-based builder access, so that I automatically get the benefits of the new API.

#### Acceptance Criteria

1. WHEN the source generator creates a table class THEN it SHALL generate method-based builder access methods
2. WHEN a table has a single key THEN the generator SHALL create Get(pk), Update(pk), Delete(pk) methods
3. WHEN a table has a composite key THEN the generator SHALL create Get(pk, sk), Update(pk, sk), Delete(pk, sk) methods
4. WHEN a table has indexes THEN the generator SHALL create appropriate Query() methods on index properties

### Requirement 8

**User Story:** As a library consumer, I want clear documentation on the new method-based API, so that I understand when and how to use each pattern.

#### Acceptance Criteria

1. WHEN reading API documentation THEN examples SHALL show the method-based pattern as the primary approach
2. WHEN viewing migration guides THEN clear before/after examples SHALL be provided
3. WHEN learning about LINQ preparation THEN the documentation SHALL explain how method-based access enables future LINQ support
4. WHEN using IntelliSense THEN method signatures SHALL include helpful parameter names and documentation

### Requirement 9

**User Story:** As a library consumer, I want the new API to maintain AOT compatibility, so that I can continue using the library in Native AOT applications.

#### Acceptance Criteria

1. WHEN using method-based builders THEN they SHALL not rely on reflection or dynamic code generation
2. WHEN the source generator creates methods THEN the generated code SHALL be AOT-safe
3. WHEN compiling for Native AOT THEN no warnings or errors SHALL be produced
4. WHEN running in AOT mode THEN all method-based operations SHALL execute correctly

### Requirement 10

**User Story:** As a library consumer, I want index queries to be simpler without complex generic type parameters, so that working with indexes is more intuitive.

#### Acceptance Criteria

1. WHEN defining an index THEN the generic type parameter SHALL be optional
2. WHEN querying an index THEN the result type SHALL be specified at query time rather than index definition time
3. WHEN using DynamoDbIndex&lt;T&gt; THEN it SHALL still be supported for backward compatibility
4. WHEN using non-generic DynamoDbIndex THEN Query() SHALL return a standard QueryRequestBuilder
