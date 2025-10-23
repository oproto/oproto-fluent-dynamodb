# Requirements Document

## Introduction

This document specifies requirements for the Projection Models feature in Oproto.FluentDynamoDb. The feature enables automatic generation and application of DynamoDB projection expressions based on entity types, reducing boilerplate code, preventing common mistakes, and optimizing query costs by fetching only required data. The system will use source generation to create type-safe projection mappings and provide compile-time validation for GSI projection constraints.

## Glossary

- **Projection_System**: The complete projection models feature including source generation, runtime application, and validation
- **Projection_Model**: A C# class decorated with [DynamoDbProjection] that defines a subset of properties from a source entity
- **Source_Entity**: The full DynamoDB entity class that a Projection_Model derives from
- **Projection_Expression**: A DynamoDB expression string specifying which attributes to retrieve (e.g., "id, amount, created_date")
- **Source_Generator**: The Roslyn-based code generator that analyzes Projection_Models and generates mapping code
- **GSI**: Global Secondary Index in DynamoDB
- **Discriminator**: A property value used to identify which entity type a DynamoDB item represents when multiple schemas exist in the same table
- **Hydration**: The process of populating a Projection_Model instance with data from DynamoDB response
- **Shared_GSI**: A GSI that is used by multiple entity types in the same table, identified by having the same index name across entities
- **Table_Level_Projection**: A projection definition that applies to all entity types querying a specific GSI

## Requirements

### Requirement 1

**User Story:** As a developer, I want to define projection models using attributes, so that I can specify which properties to retrieve without writing projection expression strings manually

#### Acceptance Criteria

1. WHEN a developer decorates a class with [DynamoDbProjection(typeof(SourceEntity))], THE Projection_System SHALL recognize the class as a Projection_Model
2. WHEN the Source_Generator processes a Projection_Model, THE Source_Generator SHALL validate that all properties in the Projection_Model exist on the Source_Entity
3. WHEN the Source_Generator processes a Projection_Model, THE Source_Generator SHALL generate a projection expression string containing all property names mapped to their DynamoDB attribute names
4. WHEN a Projection_Model property does not exist on the Source_Entity, THE Source_Generator SHALL emit a compilation error with the property name and Source_Entity type
5. WHEN a Projection_Model is defined as a partial class, THE Source_Generator SHALL generate the complementary partial class with mapping metadata

### Requirement 2

**User Story:** As a developer, I want projection expressions to be applied automatically when I query with a projection model type, so that I do not need to manually specify projection expressions

#### Acceptance Criteria

1. WHEN a developer calls ToListAsync<TProjection>() WHERE TProjection is a Projection_Model, THE Projection_System SHALL automatically apply the generated projection expression to the query
2. WHEN a developer calls ToListAsync<TProjection>(), THE Projection_System SHALL hydrate only the properties defined in TProjection from the DynamoDB response
3. WHEN a query returns items, THE Projection_System SHALL map DynamoDB attribute names to Projection_Model property names using the generated mapping
4. WHEN a developer manually specifies a projection expression using ProjectionExpression() method, THE Projection_System SHALL use the manual expression instead of the auto-generated one
5. WHEN ToListAsync<TEntity>() is called WHERE TEntity is not a Projection_Model, THE Projection_System SHALL not apply any automatic projection expression

### Requirement 3

**User Story:** As a developer, I want GSI instances to be auto-generated with their projection requirements, so that I can access indexes through a type-safe API without manual instantiation

#### Acceptance Criteria

1. WHEN a [GlobalSecondaryIndex] attribute is defined on an entity property, THE Source_Generator SHALL generate a DynamoDbIndex property on the table class
2. WHEN a GSI has a [UseProjection] attribute, THE Source_Generator SHALL generate the index property with the projection type constraint
3. WHEN a developer accesses table.Gsi1, THE Projection_System SHALL provide a DynamoDbIndex instance configured with the correct index name and projection
4. WHEN multiple entities share the same GSI name, THE Source_Generator SHALL generate a single index property that works for all entity types
5. WHEN a GSI projects only specific attributes, THE generated index SHALL automatically apply the projection expression to all queries regardless of entity type

### Requirement 3a

**User Story:** As a developer, I want GSI projections to apply uniformly across all entity types, so that entities sharing a GSI automatically use the same projection without duplication

#### Acceptance Criteria

1. WHEN multiple entity types define the same GSI name with [GlobalSecondaryIndex], THE Projection_System SHALL recognize them as sharing the same physical GSI
2. WHEN a shared GSI has a projection defined, THE Projection_System SHALL apply that projection to queries for any entity type using that GSI
3. WHEN querying table.StatusIndex with different entity types, THE Projection_System SHALL use the same projection expression for all entity types
4. WHEN a GSI projection is defined at the table level, THE Source_Generator SHALL not require per-entity projection definitions
5. WHEN an entity type is queried through a projected GSI, THE Projection_System SHALL only hydrate properties that exist in both the entity and the GSI projection

### Requirement 4

**User Story:** As a developer, I want projection models to work with discriminator-based multi-entity queries, so that I can retrieve projected data from tables containing multiple entity types

#### Acceptance Criteria

1. WHEN a query returns items with different discriminator values, THE Projection_System SHALL identify the correct entity type for each item using the discriminator property
2. WHEN a Projection_Model is defined for an entity type with a discriminator, THE Projection_System SHALL include the discriminator property in the projection expression
3. WHEN hydrating a Projection_Model from a multi-entity query result, THE Projection_System SHALL verify the discriminator value matches the expected Source_Entity type
4. WHEN a discriminator value does not match any known Projection_Model, THE Projection_System SHALL skip the item or throw an exception based on configuration
5. WHEN multiple Projection_Models are used in a single query result, THE Projection_System SHALL correctly hydrate each item to its corresponding Projection_Model type

### Requirement 5

**User Story:** As a developer, I want clear compilation errors when projection models are misconfigured, so that I can identify and fix issues before runtime

#### Acceptance Criteria

1. WHEN a Projection_Model property type differs from the Source_Entity property type, THE Source_Generator SHALL emit a compilation error with both type names
2. WHEN a Projection_Model references a non-existent Source_Entity type, THE Source_Generator SHALL emit a compilation error with the invalid type name
3. WHEN a GSI [UseProjection] references a non-existent Projection_Model, THE Source_Generator SHALL emit a compilation error
4. WHEN a Projection_Model is not declared as partial, THE Source_Generator SHALL emit a compilation warning suggesting the partial keyword
5. WHEN the Source_Generator encounters any validation error, THE error message SHALL include the file path, line number, and suggested fix

### Requirement 6

**User Story:** As a developer, I want projection models to integrate seamlessly with existing query builders, so that I can adopt the feature incrementally without breaking existing code

#### Acceptance Criteria

1. WHEN existing code uses manual ProjectionExpression() calls, THE Projection_System SHALL continue to work without modification
2. WHEN a developer uses ToListAsync<TEntity>() with a full entity type, THE Projection_System SHALL behave identically to the current implementation
3. WHEN projection models are added to a project, THE Projection_System SHALL not affect queries that do not use Projection_Models
4. WHEN a developer mixes manual and automatic projections in the same codebase, THE Projection_System SHALL handle both approaches correctly
5. WHEN upgrading to a version with projection models, THE Projection_System SHALL maintain backward compatibility with all existing query patterns

### Requirement 7

**User Story:** As a developer, I want the table class to have auto-generated index properties, so that I can eliminate manual DynamoDbIndex instantiation and reduce boilerplate code

#### Acceptance Criteria

1. WHEN the Source_Generator processes a table with GSI definitions, THE Source_Generator SHALL generate properties for each unique GSI name on the table class
2. WHEN a generated index property is accessed, THE property SHALL return a configured DynamoDbIndex instance with the correct index name
3. WHEN the legacy manual DynamoDbIndex instantiation pattern exists, THE Projection_System SHALL continue to support it for backward compatibility
4. WHEN multiple entities define the same GSI name, THE Source_Generator SHALL generate only one index property for that GSI
5. WHEN a table has no GSI definitions, THE Source_Generator SHALL not generate any index properties

### Requirement 7a

**User Story:** As a developer not using source generation, I want to manually configure GSI projections, so that I can use projection features without generated code

#### Acceptance Criteria

1. WHEN a developer manually instantiates a DynamoDbIndex, THE Projection_System SHALL provide a configuration API to specify projection expressions
2. WHEN manual projection configuration is used, THE Projection_System SHALL apply the projection to all queries through that index instance
3. WHEN breaking API changes are required for manual configuration, THE Projection_System SHALL provide clear migration documentation
4. WHEN a developer uses manual configuration, THE Projection_System SHALL validate projection expressions at runtime
5. WHEN both manual and generated configurations exist for the same GSI, THE Projection_System SHALL prioritize the manual configuration

### Requirement 8

**User Story:** As a developer, I want projection models to optimize DynamoDB costs, so that I retrieve only the data I need and reduce read capacity consumption

#### Acceptance Criteria

1. WHEN a Projection_Model contains fewer properties than the Source_Entity, THE Projection_System SHALL generate a projection expression that requests only those properties
2. WHEN a query executes with a Projection_Model, THE Projection_System SHALL reduce the amount of data transferred from DynamoDB compared to retrieving the full entity
3. WHEN the Source_Generator creates a projection expression, THE expression SHALL include only the minimum required attributes for the Projection_Model
4. WHEN a Projection_Model is used in a query, THE Projection_System SHALL not retrieve attributes that are not defined in the Projection_Model
5. WHEN measuring query performance, THE Projection_System SHALL demonstrate measurable reduction in consumed read capacity units for projected queries compared to full entity queries
