# Unit Test Migration Status

## Overview

This document tracks the migration of brittle unit tests from string-based assertions to compilation verification and semantic assertions. The migration aims to make tests more resilient to formatting changes while maintaining full test coverage.

**Last Updated:** 2025-10-22  
**Total Test Files:** 15  
**Total Tests:** 164  
**Migrated Tests:** 47 (28.7%)

## Migration Priority Classification

### Priority 1: High Impact (Core Generator Tests)
These tests verify core code generation logic and break frequently on formatting changes.

### Priority 2: Medium Impact (Supporting Generator Tests)
These tests verify supporting functionality and occasionally break on formatting changes.

### Priority 3: Low Impact (Infrastructure & Model Tests)
These tests verify infrastructure, diagnostics, or simple models and rarely break.

---

## Test File Analysis

### Priority 1: High Impact Tests

#### 1. MapperGeneratorTests.cs ✅ COMPLETED
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Generators/MapperGeneratorTests.cs`
- **Total Tests:** 9
- **Migration Status:** ✅ **COMPLETED** (2025-10-22)
- **Compilation Verification:** ✅ Already Added
- **Migration Changes Applied:**
  - ✅ Replaced method existence checks with `.ShouldContainMethod()`
  - ✅ Replaced assignment checks with `.ShouldContainAssignment()`
  - ✅ Replaced LINQ checks with `.ShouldUseLinqMethod()`
  - ✅ Replaced type reference checks with `.ShouldReferenceType()`
  - ✅ Preserved DynamoDB attribute type checks (S, N, SS, NS, L, M) with "because" messages
  - ✅ Preserved null handling checks with "because" messages
  - ✅ Preserved relationship mapping checks with "because" messages
  - ✅ Added file header comment documenting migration
  - ✅ All 9 tests passing
- **Notes:** 
  - Migration completed successfully
  - Tests now resilient to formatting changes
  - DynamoDB-specific behavior checks preserved
  - Helper methods `CreateEntitySource()` and `CreateRelatedEntitySources()` remain unchanged

**Test Breakdown:**
1. `GenerateEntityImplementation_WithBasicEntity_ProducesCorrectCode` - Basic entity mapping
2. `GenerateEntityImplementation_WithMultiItemEntity_GeneratesMultiItemMethods` - Multi-item entities
3. `GenerateEntityImplementation_WithRelatedEntities_GeneratesRelationshipMapping` - Relationship mapping
4. `GenerateEntityImplementation_WithGsiProperties_GeneratesCorrectMetadata` - GSI metadata
5. `GenerateEntityImplementation_WithNullableProperties_GeneratesNullChecks` - Nullable handling
6. `GenerateEntityImplementation_WithCollectionProperties_GeneratesNativeDynamoDbCollections` - Collection mapping
7. `GenerateEntityImplementation_WithDifferentPropertyTypes_GeneratesCorrectConversions` - Type conversions
8. `GenerateEntityImplementation_WithEntityDiscriminator_GeneratesDiscriminatorLogic` - Discriminator logic
9. `GenerateEntityImplementation_WithErrorHandling_GeneratesExceptionHandling` - Error handling

---

#### 2. AdvancedTypeGenerationTests.cs ✅ COMPLETED
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Generators/AdvancedTypeGenerationTests.cs`
- **Total Tests:** 38
- **Migration Status:** ✅ **COMPLETED** (2025-10-22)
- **Compilation Verification:** ✅ Already Added
- **Migration Changes Applied:**
  - ✅ Compilation verification already present in all tests
  - ✅ Replaced method existence checks with `.ShouldContainMethod()`
  - ✅ Replaced assignment checks with `.ShouldContainAssignment()`
  - ✅ Replaced LINQ checks with `.ShouldUseLinqMethod()`
  - ✅ Replaced type reference checks with `.ShouldReferenceType()`
  - ✅ Preserved DynamoDB attribute type checks (S, N, SS, NS, BS, L, M) with "because" messages
  - ✅ Preserved null and empty collection handling checks with "because" messages
  - ✅ Preserved JSON serialization checks with "because" messages
  - ✅ Preserved blob storage checks with "because" messages
  - ✅ Added file header comment documenting migration
  - ✅ All 38 tests passing
- **Notes:** 
  - Migration completed successfully
  - Tests now resilient to formatting changes
  - DynamoDB-specific behavior checks preserved for:
    - Dictionary/Map conversions (M attribute type)
    - HashSet/Set conversions (SS, NS, BS attribute types)
    - List conversions (L attribute type)
    - TTL Unix epoch conversions (N attribute type)
    - JSON blob serialization (System.Text.Json and Newtonsoft.Json)
    - Blob reference storage (async methods, IBlobStorageProvider)
  - Helper method `GenerateCode()` with multiple configuration options remains unchanged

**Test Categories:**
- Map Property Tests (Task 19.1): 4 tests ✅
- Set Property Tests (Task 19.2): 4 tests ✅
- List Property Tests (Task 19.3): 4 tests ✅
- TTL Property Tests (Task 19.4): 5 tests ✅
- JSON Blob Property Tests (Task 19.5): 10 tests ✅
- Blob Reference Property Tests (Task 19.6): 5 tests ✅
- Compilation Error Diagnostics Tests (Task 19.7): 6 tests ✅

---

#### 3. KeysGeneratorTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Generators/KeysGeneratorTests.cs`
- **Total Tests:** 8
- **Migration Status:** ❌ Not Started
- **Compilation Verification:** ✅ Already Added
- **String Assertions Found:**
  - Method existence checks: `Should().Contain("public static string Pk(")`
  - Key format checks: `Should().Contain("var keyValue = \"tenant#\" + id")`
  - GSI key builder checks: `Should().Contain("public static partial class StatusIndexKeys")`
  - Null check generation: `Should().Contain("if (id == null)")`
- **Estimated Effort:** Medium (8 tests with format string checks)
- **Notes:**
  - Already has compilation verification in all tests
  - Key format strings are DynamoDB-specific and should be preserved
  - Tests verify partition key, sort key, and GSI key generation

**Test Breakdown:**
1. `GenerateKeysClass_WithPartitionKeyOnly_GeneratesPartitionKeyBuilder`
2. `GenerateKeysClass_WithPartitionAndSortKey_GeneratesAllKeyBuilders`
3. `GenerateKeysClass_WithGsi_GeneratesGsiKeyBuilders`
4. `GenerateKeysClass_WithNullableTypes_GeneratesNullChecks`
5. `GenerateKeysClass_WithGuidType_GeneratesToStringConversion`
6. `GenerateKeysClass_WithDateTimeType_GeneratesFormattedString`
7. `GenerateKeysClass_WithNoKeys_GeneratesEmptyClass`
8. `GenerateKeysClass_WithCustomKeyFormat_ParsesFormatCorrectly`

---

### Priority 2: Medium Impact Tests

#### 4. FieldsGeneratorTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Generators/FieldsGeneratorTests.cs`
- **Total Tests:** 6
- **Migration Status:** ❌ Not Started
- **Compilation Verification:** ✅ Already Added
- **String Assertions Found:**
  - Constant field checks: `Should().Contain("public const string Id = \"pk\"")`
  - Nested class checks: `Should().Contain("public static partial class TestGSIFields")`
  - Reserved word handling: `Should().Contain("public const string @class")`
- **Estimated Effort:** Low (6 tests, mostly constant values)
- **Notes:**
  - Already has compilation verification in all tests
  - Field constant values are DynamoDB attribute names and should be preserved
  - Tests verify field name generation and GSI nested classes

**Test Breakdown:**
1. `GenerateFieldsClass_WithBasicEntity_ProducesCorrectCode`
2. `GenerateFieldsClass_WithGsiProperties_GeneratesNestedGsiClasses`
3. `GenerateFieldsClass_WithReservedWords_HandlesCorrectly`
4. `GenerateFieldsClass_WithNoAttributeMappings_GeneratesEmptyClass`
5. `GenerateFieldsClass_WithComplexGsiName_GeneratesSafeClassName`
6. `GenerateFieldsClass_WithMultipleGsis_GeneratesAllNestedClasses`

---

#### 5. DynamoDbSourceGeneratorTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/DynamoDbSourceGeneratorTests.cs`
- **Total Tests:** 3
- **Migration Status:** ❌ Not Started
- **Compilation Verification:** ❌ Not Added
- **String Assertions Found:**
  - Class existence checks: `Should().Contain("public partial class TestEntity")`
  - Namespace checks: `Should().Contain("namespace TestNamespace")`
  - Method existence checks: `Should().Contain("public static string Pk(")`
  - Constant field checks: `Should().Contain("public const string Id")`
- **Estimated Effort:** Low (3 end-to-end tests)
- **Notes:**
  - End-to-end generator tests
  - Should add compilation verification
  - Tests verify complete generator output (Entity + Fields + Keys)

**Test Breakdown:**
1. `Generator_WithBasicEntity_ProducesCode`
2. `Generator_WithoutDynamoDbTableAttribute_ProducesNoCode`
3. `Generator_WithGsiEntity_GeneratesFieldsWithGsiClasses`

---

#### 6. MapperGeneratorBugFixTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Generators/MapperGeneratorBugFixTests.cs`
- **Total Tests:** 1
- **Migration Status:** ❌ Not Started
- **Compilation Verification:** ❌ Not Added
- **String Assertions Found:**
  - Type reference checks: `Should().Contain("typeof(TestEntity)")`
  - Negative checks: `Should().NotContain("typeof(Id)")`
- **Estimated Effort:** Very Low (1 bug fix verification test)
- **Notes:**
  - Verifies specific bug fix for CS0246 error
  - Should keep string checks for bug verification
  - Should add compilation verification

**Test Breakdown:**
1. `GenerateEntityImplementation_WithPropertyNames_UsesEntityClassNameInTypeofExpressions`

---

### Priority 3: Low Impact Tests

#### 7. EntityAnalyzerTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/EntityAnalyzerTests.cs`
- **Total Tests:** 10
- **Migration Status:** ✅ Good As-Is
- **Compilation Verification:** ❌ Not Needed
- **String Assertions Found:** None (diagnostic tests only)
- **Estimated Effort:** None (no migration needed)
- **Notes:**
  - Tests verify diagnostic generation (errors and warnings)
  - Uses `Diagnostics.Should().Contain(d => d.Id == "DYNDB021")`
  - No code generation verification needed
  - Already follows best practices

---

#### 8. EdgeCaseTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/EdgeCases/EdgeCaseTests.cs`
- **Total Tests:** 12
- **Migration Status:** 🔍 Review Needed
- **Compilation Verification:** Unknown
- **Estimated Effort:** Low-Medium (depends on test content)
- **Notes:**
  - Need to review test content to determine if migration is beneficial
  - Edge case tests may already be well-structured

---

#### 9. DiagnosticDescriptorsTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Diagnostics/DiagnosticDescriptorsTests.cs`
- **Total Tests:** 22
- **Migration Status:** ✅ Good As-Is
- **Compilation Verification:** ❌ Not Needed
- **String Assertions Found:** Minimal (only for message format verification)
- **Estimated Effort:** None (no migration needed)
- **Notes:**
  - Tests verify diagnostic descriptor properties
  - Uses `MessageFormat.ToString().Should().Contain()` for message verification
  - This is appropriate for diagnostic message testing

---

#### 10. EntityModelTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Models/EntityModelTests.cs`
- **Total Tests:** 8
- **Migration Status:** ✅ Good As-Is
- **Compilation Verification:** ❌ Not Needed
- **Estimated Effort:** None (no migration needed)
- **Notes:**
  - Tests verify model structure and properties
  - No code generation involved

---

#### 11. PropertyModelTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Models/PropertyModelTests.cs`
- **Total Tests:** 8
- **Migration Status:** ✅ Good As-Is
- **Compilation Verification:** ❌ Not Needed
- **Estimated Effort:** None (no migration needed)
- **Notes:**
  - Tests verify model structure and properties
  - No code generation involved

---

#### 12. RelationshipModelTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Models/RelationshipModelTests.cs`
- **Total Tests:** 10
- **Migration Status:** ✅ Good As-Is
- **Compilation Verification:** ❌ Not Needed
- **Estimated Effort:** None (no migration needed)
- **Notes:**
  - Tests verify model structure and properties
  - No code generation involved

---

#### 13. EndToEndSourceGeneratorTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Integration/EndToEndSourceGeneratorTests.cs`
- **Total Tests:** 11
- **Migration Status:** 🔍 Review Needed
- **Compilation Verification:** Unknown
- **Estimated Effort:** Low-Medium (depends on test content)
- **Notes:**
  - Integration tests for end-to-end scenarios
  - May already have good test structure

---

#### 14. SourceGeneratorPerformanceTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/Performance/SourceGeneratorPerformanceTests.cs`
- **Total Tests:** 8
- **Migration Status:** ✅ Good As-Is
- **Compilation Verification:** ❌ Not Needed
- **String Assertions Found:** Minimal (only for verification of generated output)
- **Estimated Effort:** None (no migration needed)
- **Notes:**
  - Performance tests focus on timing and diagnostics
  - String checks are minimal and appropriate

---

#### 15. SemanticAssertionsTests.cs
- **Location:** `Oproto.FluentDynamoDb.SourceGenerator.UnitTests/TestHelpers/SemanticAssertionsTests.cs`
- **Total Tests:** 10
- **Migration Status:** ✅ Good As-Is
- **Compilation Verification:** ❌ Not Needed
- **Estimated Effort:** None (no migration needed)
- **Notes:**
  - Tests verify the SemanticAssertions helper itself
  - Uses string assertions to verify error messages
  - This is appropriate for testing the test infrastructure

---

## Summary Statistics

### By Priority
- **Priority 1 (High Impact):** 3 files, 55 tests
- **Priority 2 (Medium Impact):** 3 files, 10 tests
- **Priority 3 (Low Impact):** 9 files, 99 tests

### By Migration Status
- **Completed:** 2 files, 47 tests ✅
- **Not Started:** 4 files, 18 tests
- **Good As-Is:** 7 files, 87 tests
- **Review Needed:** 2 files, 23 tests

### By Compilation Verification Status
- **Already Added:** 3 files (MapperGeneratorTests, AdvancedTypeGenerationTests, KeysGeneratorTests, FieldsGeneratorTests)
- **Needs Adding:** 2 files (DynamoDbSourceGeneratorTests, MapperGeneratorBugFixTests)
- **Not Needed:** 10 files (diagnostic, model, and infrastructure tests)

---

## Migration Checklist Template

Use this checklist for each file being migrated:

```markdown
## [FileName].cs

- [ ] Added compilation verification to all tests
- [ ] Replaced method existence checks with `.ShouldContainMethod()`
- [ ] Replaced assignment checks with `.ShouldContainAssignment()`
- [ ] Replaced LINQ checks with `.ShouldUseLinqMethod()`
- [ ] Replaced type reference checks with `.ShouldReferenceType()`
- [ ] Added "because" messages to DynamoDB-specific checks
- [ ] All tests pass
- [ ] Verified tests catch intentional errors
- [ ] Verified tests pass with formatting changes
- [ ] Added file header comment documenting migration
```

---

## String Assertion Patterns Found

### Patterns to Replace with Semantic Assertions

1. **Method Existence:**
   ```csharp
   // Before
   code.Should().Contain("public static string Pk(");
   
   // After
   code.ShouldContainMethod("Pk");
   ```

2. **Assignment Checks:**
   ```csharp
   // Before
   code.Should().Contain("entity.Id = ");
   
   // After
   code.ShouldContainAssignment("entity.Id");
   ```

3. **LINQ Usage:**
   ```csharp
   // Before
   code.Should().Contain(".Select(");
   
   // After
   code.ShouldUseLinqMethod("Select");
   ```

4. **Type References:**
   ```csharp
   // Before
   code.Should().Contain("typeof(TestEntity)");
   
   // After
   code.ShouldReferenceType("TestEntity");
   ```

### Patterns to Keep (DynamoDB-Specific)

1. **Attribute Types:**
   ```csharp
   // Keep with "because" message
   code.Should().Contain("S =", "should use String type for string properties");
   code.Should().Contain("N =", "should use Number type for numeric properties");
   code.Should().Contain("SS =", "should use String Set for HashSet<string>");
   code.Should().Contain("NS =", "should use Number Set for HashSet<int>");
   code.Should().Contain("L =", "should use List type for List<T>");
   code.Should().Contain("M =", "should use Map type for Dictionary<,>");
   ```

2. **Null Handling:**
   ```csharp
   // Keep with "because" message
   code.Should().Contain("!= null", "should check for null before adding to DynamoDB item");
   code.Should().Contain("Count > 0", "should check for empty collections before adding to DynamoDB item");
   ```

3. **Format Strings:**
   ```csharp
   // Keep with "because" message
   code.Should().Contain("var keyValue = \"tenant#\" + id", "should use correct partition key format");
   ```

---

## Next Steps

1. ✅ Complete this migration status document
2. ✅ **COMPLETED:** Priority 1: MapperGeneratorTests.cs (9 tests migrated)
3. ✅ **COMPLETED:** Priority 1: AdvancedTypeGenerationTests.cs (38 tests migrated)
4. ⏭️ Continue with Priority 1: KeysGeneratorTests.cs (8 tests)
5. ⏭️ Move to Priority 2 files
6. ⏭️ Review Priority 3 files that need review
7. ⏭️ Create final migration summary report
