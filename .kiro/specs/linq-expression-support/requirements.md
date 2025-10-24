# Requirements Document

## Introduction

This feature adds LINQ-style expression support to the FluentDynamoDb library, enabling developers to write type-safe queries using C# lambda expressions instead of string-based expressions. The implementation must be AOT-compatible, respect entity metadata constraints, and support all DynamoDB query operators. This spec focuses on query, filter, and condition expressions; update expressions will be addressed in a future spec.

## Glossary

- **Expression System**: The C# expression tree analysis and translation mechanism that converts lambda expressions to DynamoDB query syntax
- **Query Builder**: The fluent API components (QueryRequestBuilder, ScanRequestBuilder) that construct DynamoDB requests
- **Entity Metadata**: Runtime information about entity properties, including attribute mappings, computed values, and extracted attributes
- **AOT Compilation**: Ahead-of-Time compilation that requires all code paths to be statically analyzable
- **DynamoDB Operators**: Query and filter operators supported by DynamoDB (=, <, >, <=, >=, BETWEEN, begins_with, contains, etc.)
- **Computed Attribute**: A property whose value is derived from other properties and stored as a separate DynamoDB attribute
- **Extracted Attribute**: A property value extracted from a complex object (like JSON) and stored separately for querying
- **Key Condition Expression**: The WHERE clause for partition key and sort key in Query operations
- **Filter Expression**: Additional filtering applied after the query/scan retrieves items

## Requirements

### Requirement 1: Type-Safe Query Expression API

**User Story:** As a developer, I want to write queries using C# lambda expressions, so that I can leverage compile-time type checking and IntelliSense support.

#### Acceptance Criteria

1. WHEN a developer calls Query().Where() with a lambda expression, THE Expression System SHALL parse the expression tree and generate equivalent DynamoDB key condition expression syntax
2. WHEN a developer calls WithFilter() with a lambda expression on Query or Scan builders, THE Expression System SHALL parse the expression tree and generate equivalent DynamoDB filter expression syntax
3. WHEN a developer calls WithCondition() with a lambda expression, THE Expression System SHALL parse the expression tree and generate equivalent DynamoDB condition expression syntax
4. WHEN a developer uses property access in an expression, THE Expression System SHALL validate that the property maps to a queryable DynamoDB attribute
5. THE Expression System SHALL provide overloads that accept Expression<Func<TEntity, bool>> alongside existing string-based and formatting string methods
6. THE Expression System SHALL coexist with existing string-based expression methods without breaking changes

### Requirement 2: AOT-Safe Expression Analysis

**User Story:** As a developer deploying to Native AOT environments, I want expression-based queries to work without runtime code generation, so that my application remains AOT-compatible.

#### Acceptance Criteria

1. WHEN the Expression System analyzes expression trees, THE Expression System SHALL use only statically analyzable code paths without dynamic code generation
2. WHEN the Expression System encounters unsupported expression patterns, THE Expression System SHALL throw a compile-time or early-runtime exception with a clear error message
3. THE Expression System SHALL NOT use Expression.Compile() or any reflection-emit functionality
4. THE Expression System SHALL leverage source-generated entity metadata for property-to-attribute mappings
5. WHEN deployed to Native AOT, THE Expression System SHALL function identically to JIT-compiled deployments

### Requirement 3: DynamoDB Operator Support

**User Story:** As a developer, I want to use all DynamoDB query operators through C# expressions, so that I can express complex query conditions naturally.

#### Acceptance Criteria

1. WHEN a developer uses equality comparison (==), THE Expression System SHALL generate the "=" operator in DynamoDB syntax
2. WHEN a developer uses comparison operators (<, >, <=, >=), THE Expression System SHALL generate the corresponding DynamoDB comparison operators
3. WHEN a developer uses string.StartsWith(), THE Expression System SHALL generate the "begins_with" function in DynamoDB syntax
4. WHEN a developer uses a custom Between() extension method, THE Expression System SHALL generate the "BETWEEN" operator in DynamoDB syntax
5. WHEN a developer uses string.Contains(), THE Expression System SHALL generate the "contains" function in DynamoDB syntax
6. WHEN a developer uses logical AND (&&), THE Expression System SHALL combine conditions with "AND" in DynamoDB syntax
7. WHEN a developer uses logical OR (||), THE Expression System SHALL combine conditions with "OR" in DynamoDB syntax
8. WHEN a developer uses negation (!), THE Expression System SHALL generate "NOT" in DynamoDB syntax
9. THE Expression System SHALL support attribute_exists() and attribute_not_exists() through custom extension methods

### Requirement 4: Entity Metadata Validation

**User Story:** As a developer, I want the expression system to validate property access against entity metadata, so that I only query attributes that actually exist in DynamoDB.

#### Acceptance Criteria

1. WHEN a developer references a property in an expression, THE Expression System SHALL verify the property has a DynamoDB attribute mapping
2. WHEN a developer references a computed attribute, THE Expression System SHALL allow the reference and map to the computed attribute name
3. WHEN a developer references an extracted attribute, THE Expression System SHALL allow the reference and map to the extracted attribute name
4. WHEN a developer references a property without a DynamoDB mapping, THE Expression System SHALL throw an exception with a message indicating the property cannot be queried
5. WHEN a developer references a property marked as non-queryable, THE Expression System SHALL throw an exception with a message indicating the property is not queryable
6. THE Expression System SHALL use source-generated metadata to perform validation at the earliest possible point

### Requirement 5: Value Capture and Parameter Generation

**User Story:** As a developer, I want to use C# variables and constants in my expressions, so that I can write queries with dynamic values naturally.

#### Acceptance Criteria

1. WHEN a developer uses a local variable in an expression, THE Expression System SHALL capture the variable's value and generate a DynamoDB expression attribute value
2. WHEN a developer uses a constant in an expression, THE Expression System SHALL capture the constant and generate a DynamoDB expression attribute value
3. WHEN a developer uses a property from a captured closure, THE Expression System SHALL evaluate the property and generate a DynamoDB expression attribute value
4. THE Expression System SHALL NOT attempt to execute methods or functions on the DynamoDB side
5. WHEN a developer uses an unsupported value expression (like method calls), THE Expression System SHALL throw an exception with a clear error message
6. THE Expression System SHALL generate unique parameter names for expression attribute values to avoid collisions

### Requirement 6: Expression Context Validation

**User Story:** As a developer, I want the expression system to validate that my expressions are appropriate for their context, so that I catch errors early rather than at runtime from DynamoDB.

#### Acceptance Criteria

1. WHEN an expression is used in Query().Where() and references only partition key and sort key attributes, THE Expression System SHALL generate valid key condition expression syntax
2. WHEN an expression is used in Query().Where() and references non-key attributes, THE Expression System SHALL throw an exception indicating those attributes must use WithFilter()
3. WHEN an expression is used in WithFilter() on Query or Scan builders, THE Expression System SHALL generate filter expression syntax without key attribute restrictions
4. WHEN an expression is used in WithCondition(), THE Expression System SHALL generate condition expression syntax without key attribute restrictions
5. THE Expression System SHALL use entity metadata to determine which attributes are partition keys and sort keys
6. THE Expression System SHALL generate expression attribute names and values that integrate with existing parameter generation

### Requirement 7: Composite Expression Support

**User Story:** As a developer, I want to chain multiple Where() and WithFilter() calls, so that I can build complex queries incrementally.

#### Acceptance Criteria

1. WHEN a developer calls Query().Where() multiple times, THE Expression System SHALL combine the expressions with AND logic
2. WHEN a developer calls WithFilter() multiple times on Query or Scan builders, THE Expression System SHALL combine the expressions with AND logic
3. WHEN a developer mixes string-based Where() calls with expression-based Where() calls, THE Expression System SHALL combine them correctly
4. WHEN a developer mixes formatting string Where() calls with expression-based Where() calls, THE Expression System SHALL combine them correctly
5. WHEN a developer mixes string-based WithFilter() calls with expression-based WithFilter() calls, THE Expression System SHALL combine them correctly
6. THE Expression System SHALL maintain parameter name uniqueness across multiple expression calls and across different expression types

### Requirement 8: Error Handling and Diagnostics

**User Story:** As a developer, I want clear error messages when I write invalid expressions, so that I can quickly understand and fix issues.

#### Acceptance Criteria

1. WHEN an expression references an unmapped property, THE Expression System SHALL throw an exception with the property name and entity type
2. WHEN an expression uses an unsupported operator, THE Expression System SHALL throw an exception listing supported operators
3. WHEN an expression uses an unsupported method call, THE Expression System SHALL throw an exception with the method name
4. WHEN an expression is too complex to translate, THE Expression System SHALL throw an exception suggesting string-based expressions as an alternative
5. THE Expression System SHALL include the original expression in error messages for debugging purposes
6. WHEN validation fails, THE Expression System SHALL fail fast before making any DynamoDB calls

### Requirement 9: Performance and Efficiency

**User Story:** As a developer, I want expression-based queries to have minimal overhead, so that my application performance remains optimal.

#### Acceptance Criteria

1. THE Expression System SHALL cache expression tree analysis results when the same expression is used multiple times
2. THE Expression System SHALL minimize allocations during expression tree traversal
3. THE Expression System SHALL reuse parameter name generation logic from existing ParameterGenerator
4. WHEN analyzing expressions, THE Expression System SHALL complete analysis in less than 10 milliseconds for typical expressions
5. THE Expression System SHALL NOT introduce more than 5% overhead compared to equivalent string-based expressions

### Requirement 10: Documentation and Examples

**User Story:** As a developer, I want comprehensive documentation and examples for expression-based queries, so that I can adopt this feature quickly.

#### Acceptance Criteria

1. THE Expression System SHALL include XML documentation comments on all public API methods
2. THE Expression System SHALL provide code examples in documentation showing common query patterns
3. THE Expression System SHALL document which operators and methods are supported
4. THE Expression System SHALL document the difference between Query().Where() and WithFilter() in expression context
5. THE Expression System SHALL provide examples showing expression-based, string-based, and formatting string approaches side-by-side
6. THE Expression System SHALL document that update expressions are not included in this implementation and will be addressed separately
