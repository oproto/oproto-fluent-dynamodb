# Requirements Document

## Introduction

This feature enhances the discriminator system in Oproto.FluentDynamoDb to support flexible entity type identification patterns commonly used in single-table DynamoDB designs. The current implementation only supports a fixed `entity_type` attribute with exact value matching, which doesn't accommodate the variety of discriminator strategies used in real-world applications.

## Glossary

- **Discriminator**: A property or pattern used to identify the entity type in a DynamoDB item
- **Single-Table Design**: A DynamoDB design pattern where multiple entity types share the same table
- **Sort Key (SK)**: The secondary key component in DynamoDB that can contain entity type information
- **Partition Key (PK)**: The primary key component in DynamoDB
- **GSI**: Global Secondary Index - an alternate query path in DynamoDB
- **Pattern Matching**: Using wildcards to match discriminator values (e.g., "USER#*")
- **Source Generator**: The compile-time code generation system that creates DynamoDB mapping code
- **Projection Model**: A subset of entity properties used for efficient queries

## Requirements

### Requirement 1: Flexible Discriminator Property Configuration

**User Story:** As a developer using single-table design, I want to specify which DynamoDB attribute contains the entity type discriminator, so that I can use sort keys, partition keys, or custom attributes for entity identification.

#### Acceptance Criteria

1. WHEN a developer applies DynamoDbTableAttribute to an entity class, THE Source Generator SHALL support a DiscriminatorProperty parameter that accepts any valid DynamoDB attribute name
2. WHEN DiscriminatorProperty is set to a value other than "entity_type", THE Source Generator SHALL generate validation code that checks the specified property
3. WHEN DiscriminatorProperty is null or empty, THE Source Generator SHALL skip discriminator validation for that entity
4. WHEN a developer specifies DiscriminatorProperty as "SK" or "PK", THE Source Generator SHALL generate code that validates against the sort key or partition key respectively

### Requirement 2: Pattern-Based Discriminator Matching

**User Story:** As a developer implementing composite key patterns, I want to use wildcard patterns to match discriminator values, so that I can identify entities with prefixed or suffixed sort keys like "USER#123" or "TENANT#abc#USER#123".

#### Acceptance Criteria

1. WHEN a developer specifies DiscriminatorPattern with a trailing wildcard (e.g., "USER#*"), THE Source Generator SHALL generate code using StartsWith matching
2. WHEN a developer specifies DiscriminatorPattern with a leading wildcard (e.g., "*#USER"), THE Source Generator SHALL generate code using EndsWith matching
3. WHEN a developer specifies DiscriminatorPattern with wildcards at both ends (e.g., "*#USER#*"), THE Source Generator SHALL generate code using Contains matching
4. WHEN a developer specifies DiscriminatorPattern without wildcards, THE Source Generator SHALL generate code using exact match comparison
5. WHEN a developer specifies both DiscriminatorValue and DiscriminatorPattern, THE Source Generator SHALL use DiscriminatorValue and ignore DiscriminatorPattern

### Requirement 3: GSI-Specific Discriminator Configuration

**User Story:** As a developer querying entities through Global Secondary Indexes, I want to specify different discriminator strategies for GSI queries, so that I can correctly identify entities when GSI keys use different patterns than primary keys.

#### Acceptance Criteria

1. WHEN a developer applies GlobalSecondaryIndexAttribute with DiscriminatorProperty, THE Source Generator SHALL generate GSI-specific discriminator validation code
2. WHEN a GSI-specific discriminator is configured, THE Source Generator SHALL use it instead of the entity-level discriminator for that GSI
3. WHEN a GSI-specific discriminator is not configured, THE Source Generator SHALL fall back to the entity-level discriminator
4. WHEN generating projection expressions for GSI queries, THE Source Generator SHALL include the GSI-specific discriminator property in the projection

### Requirement 4: Backward Compatibility with Legacy EntityDiscriminator

**User Story:** As a developer with existing code using EntityDiscriminator, I want my code to continue working without changes, so that I can upgrade the library without breaking my application.

#### Acceptance Criteria

1. WHEN a developer uses the EntityDiscriminator property, THE Source Generator SHALL treat it as equivalent to DiscriminatorProperty="entity_type" with DiscriminatorValue set to the EntityDiscriminator value
2. WHEN a developer uses both EntityDiscriminator and the new discriminator properties, THE Source Generator SHALL prioritize the new properties and mark EntityDiscriminator as obsolete
3. WHEN generating code for entities using EntityDiscriminator, THE Source Generator SHALL produce functionally identical validation logic to previous versions
4. WHEN a developer compiles code using EntityDiscriminator, THE Compiler SHALL emit an obsolescence warning directing them to use the new properties

### Requirement 5: Discriminator Validation in Projection Hydration

**User Story:** As a developer querying multi-entity tables, I want projection hydration to validate discriminators and throw clear exceptions for mismatches, so that I can detect data inconsistencies and type mismatches early.

#### Acceptance Criteria

1. WHEN the FromDynamoDb method hydrates a projection, THE Generated Code SHALL validate the discriminator before mapping properties
2. WHEN a discriminator validation fails, THE Generated Code SHALL throw DiscriminatorMismatchException with the expected and actual discriminator values
3. WHEN a discriminator property is missing from the item, THE Generated Code SHALL throw DiscriminatorMismatchException indicating the property was not found
4. WHEN no discriminator is configured for an entity, THE Generated Code SHALL skip discriminator validation and proceed with property mapping

### Requirement 6: Discriminator Property Inclusion in Projections

**User Story:** As a developer using projections for efficient queries, I want discriminator properties automatically included in projection expressions, so that validation can occur without manual configuration.

#### Acceptance Criteria

1. WHEN generating a projection expression, THE Source Generator SHALL include the discriminator property in the attribute list
2. WHEN a projection has both entity-level and GSI-specific discriminators, THE Source Generator SHALL include both properties in the projection expression
3. WHEN a discriminator property is already included in the projection properties, THE Source Generator SHALL not duplicate it in the projection expression
4. WHEN no discriminator is configured, THE Source Generator SHALL not add any discriminator-related properties to the projection expression

### Requirement 7: Compile-Time Pattern Analysis and Optimization

**User Story:** As a developer concerned about runtime performance, I want discriminator patterns analyzed at compile time and converted to optimal runtime checks, so that validation has minimal performance overhead.

#### Acceptance Criteria

1. WHEN the Source Generator analyzes a discriminator pattern, THE Source Generator SHALL determine the optimal matching strategy (StartsWith, EndsWith, Contains, or ExactMatch) at compile time
2. WHEN generating discriminator validation code, THE Source Generator SHALL use the most efficient string comparison method for the pattern
3. WHEN a pattern contains multiple wildcards in complex positions, THE Source Generator SHALL generate appropriate validation logic without using regular expressions
4. WHEN generating validation code, THE Source Generator SHALL produce code that performs zero allocations during discriminator matching

### Requirement 8: Clear Error Messages for Configuration Issues

**User Story:** As a developer configuring discriminators, I want clear compile-time errors when I misconfigure attributes, so that I can quickly identify and fix configuration problems.

#### Acceptance Criteria

1. WHEN a developer specifies both DiscriminatorValue and DiscriminatorPattern, THE Source Generator SHALL emit a diagnostic warning indicating mutual exclusivity
2. WHEN a developer specifies DiscriminatorValue or DiscriminatorPattern without DiscriminatorProperty, THE Source Generator SHALL emit a diagnostic error
3. WHEN a developer specifies an invalid pattern syntax, THE Source Generator SHALL emit a diagnostic error with guidance on correct pattern format
4. WHEN diagnostic messages are emitted, THE Messages SHALL include the entity class name and specific configuration issue
