# Requirements Document

## Introduction

This specification addresses the migration of brittle unit tests in the source generator test suite. Currently, many tests use string matching to verify generated code, which makes them fragile and prone to breaking on formatting changes. The goal is to migrate these tests to use compilation verification and semantic assertions, making them more maintainable while preserving test coverage.

## Glossary

- **Source Generator Tests**: Unit tests that verify the C# source generator produces correct code
- **String Matching**: Tests that use `.Should().Contain()` to check for exact string patterns in generated code
- **Compilation Verification**: Testing approach that verifies generated code compiles without errors using `CompilationVerifier`
- **Semantic Assertions**: Testing approach that uses syntax tree analysis to verify code structure using `SemanticAssertions`
- **Brittle Tests**: Tests that break due to non-functional changes like formatting or whitespace
- **Integration Tests**: Tests that verify end-to-end functionality with actual DynamoDB using `IntegrationTestBase`
- **Test Infrastructure**: The collection of helper classes including `CompilationVerifier`, `SemanticAssertions`, and `IntegrationTestBase`

## Requirements

### Requirement 1: Identify Brittle Tests

**User Story:** As a developer, I want to identify all tests using brittle string matching, so that I can prioritize which tests to migrate first.

#### Acceptance Criteria

1. WHEN analyzing the test suite, THE System SHALL identify all test methods that use `.Should().Contain()` for verifying generated code structure
2. WHEN categorizing identified tests, THE System SHALL classify tests by priority based on frequency of breakage and criticality of functionality
3. WHEN documenting findings, THE System SHALL create a migration plan listing high-priority, medium-priority, and low-priority test files
4. WHERE tests verify DynamoDB-specific values, THE System SHALL mark those string checks as acceptable to keep
5. WHEN identifying tests, THE System SHALL exclude tests that verify diagnostic messages or error conditions

### Requirement 2: Add Compilation Verification

**User Story:** As a developer, I want all generator tests to verify that generated code compiles, so that I can catch breaking changes early.

#### Acceptance Criteria

1. WHEN a test generates code, THE System SHALL invoke `CompilationVerifier.AssertGeneratedCodeCompiles()` after diagnostic checks
2. WHERE generated code references external types, THE System SHALL pass additional source files to the compilation verifier
3. WHEN compilation verification fails, THE System SHALL provide clear error messages with line numbers and compilation errors
4. WHILE maintaining existing assertions, THE System SHALL add compilation verification without removing other checks
5. WHEN tests already have compilation verification, THE System SHALL skip adding duplicate verification

### Requirement 3: Replace Structural String Checks

**User Story:** As a developer, I want to replace brittle string matching with semantic assertions for code structure, so that tests don't break on formatting changes.

#### Acceptance Criteria

1. WHEN tests check for method existence, THE System SHALL replace `.Should().Contain("public static MethodName")` with `.ShouldContainMethod("MethodName")`
2. WHEN tests check for assignments, THE System SHALL replace string matching with `.ShouldContainAssignment("variableName")`
3. WHEN tests check for LINQ usage, THE System SHALL replace string matching with `.ShouldUseLinqMethod("Select")` or similar
4. WHEN tests check for type references, THE System SHALL replace string matching with `.ShouldReferenceType("TypeName")`
5. WHERE semantic assertions are insufficient, THE System SHALL keep string checks with descriptive "because" messages

### Requirement 4: Preserve DynamoDB-Specific Checks

**User Story:** As a developer, I want to keep string checks for DynamoDB-specific behavior, so that I can verify correct attribute type usage.

#### Acceptance Criteria

1. WHEN tests verify DynamoDB attribute types, THE System SHALL keep string checks for "S =", "N =", "SS =", "NS =", "L =", and "M ="
2. WHEN tests verify format strings, THE System SHALL keep string checks for partition key prefixes and format patterns
3. WHEN tests verify null handling, THE System SHALL keep string checks for "!= null" and "Count > 0" conditions
4. WHEN keeping string checks, THE System SHALL add descriptive "because" messages explaining the check
5. WHILE preserving DynamoDB checks, THE System SHALL still add compilation verification and semantic assertions for structure

### Requirement 5: Update Test Documentation

**User Story:** As a developer, I want updated test documentation, so that I understand which tests have been migrated and how to write new tests.

#### Acceptance Criteria

1. WHEN migration is complete for a test file, THE System SHALL add a comment header indicating migration status
2. WHEN creating new tests, THE System SHALL follow the patterns established in migrated tests
3. WHERE test files are partially migrated, THE System SHALL document which tests remain to be migrated
4. WHEN all tests in a file are migrated, THE System SHALL update any related documentation references
5. WHILE documenting changes, THE System SHALL reference the MIGRATION_GUIDE.md for detailed patterns

### Requirement 6: Maintain Test Coverage

**User Story:** As a developer, I want to ensure test coverage is maintained during migration, so that no functionality becomes untested.

#### Acceptance Criteria

1. WHEN migrating a test, THE System SHALL verify the migrated test still validates the same functionality
2. WHERE string checks are removed, THE System SHALL ensure equivalent semantic assertions are added
3. WHEN tests are migrated, THE System SHALL run the test suite to verify all tests still pass
4. IF a test cannot be migrated without losing coverage, THE System SHALL document the reason and keep the original test
5. WHILE migrating tests, THE System SHALL not reduce the number of assertions unless they are redundant

### Requirement 7: Prioritize High-Impact Tests

**User Story:** As a developer, I want to migrate high-impact tests first, so that I get the most value from the migration effort.

#### Acceptance Criteria

1. WHEN prioritizing tests, THE System SHALL migrate tests for core mapping logic before edge case tests
2. WHEN prioritizing tests, THE System SHALL migrate tests for advanced type handling before simple type tests
3. WHEN prioritizing tests, THE System SHALL migrate tests that break frequently before stable tests
4. WHERE tests are complex and hard to understand, THE System SHALL prioritize those for migration
5. WHILE migrating, THE System SHALL complete one test file at a time before moving to the next

### Requirement 8: Validate Migration Success

**User Story:** As a developer, I want to validate that migrated tests are less brittle, so that I can confirm the migration achieved its goal.

#### Acceptance Criteria

1. WHEN migration is complete for a test, THE System SHALL intentionally modify generated code formatting to verify the test still passes
2. WHEN migration is complete for a test, THE System SHALL intentionally break generated code to verify the test catches the error
3. WHERE tests fail after migration, THE System SHALL investigate and fix the migration before proceeding
4. WHEN all tests in a file are migrated, THE System SHALL run the full test suite to verify no regressions
5. WHILE validating, THE System SHALL ensure error messages from migrated tests are clear and actionable
