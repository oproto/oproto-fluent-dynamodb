# Unit Test Fixes - Migration Documentation

This directory contains all documentation and tracking for the unit test migration project.

## Overview

The goal of this project is to migrate brittle unit tests from string-based assertions to compilation verification and semantic assertions, making them more resilient to formatting changes while maintaining full test coverage.

## Quick Start

1. **Read the Requirements:** [requirements.md](requirements.md)
2. **Review the Design:** [design.md](design.md)
3. **Check the Task List:** [tasks.md](tasks.md)
4. **Use the Quick Reference:** [MIGRATION_QUICK_REFERENCE.md](MIGRATION_QUICK_REFERENCE.md)

## Documentation Files

### Planning Documents

- **[requirements.md](requirements.md)** - EARS-compliant requirements for the migration
- **[design.md](design.md)** - Detailed design document with migration patterns and approach
- **[tasks.md](tasks.md)** - Implementation task list with priorities

### Tracking Documents

- **[MIGRATION_STATUS.md](MIGRATION_STATUS.md)** - Current status of all test files
  - Priority classification
  - Test counts and analysis
  - Migration checklist for each file
  - String assertion patterns found

- **[BASELINE_METRICS.md](BASELINE_METRICS.md)** - Baseline measurements before migration
  - Detailed metrics by priority
  - String assertion pattern analysis
  - Test infrastructure status
  - Migration effort estimation
  - Risk assessment

### Reference Documents

- **[MIGRATION_QUICK_REFERENCE.md](MIGRATION_QUICK_REFERENCE.md)** - Quick reference guide
  - Migration patterns with examples
  - Decision tree for choosing approach
  - Common mistakes to avoid
  - Validation checklist

## Tools

### Analysis Script

**[analyze-tests.sh](analyze-tests.sh)** - Automated analysis script

```bash
# Run the analysis
./.kiro/specs/unit-test-fixes/analyze-tests.sh
```

**Output:**
- Test counts by file
- String assertion counts
- Compilation verification status
- Semantic assertion status
- Pattern analysis

## Key Metrics

### Current State (Baseline)

- **Total Test Files:** 15
- **Total Tests:** 164
- **Files Requiring Migration:** 6 (Priority 1 & 2)
- **Tests Requiring Migration:** 65
- **String Assertions to Replace:** 248

### Priority Breakdown

| Priority | Files | Tests | String Assertions | Status |
|----------|-------|-------|-------------------|--------|
| Priority 1 (High) | 3 | 55 | 210 | ❌ Not Started |
| Priority 2 (Medium) | 3 | 10 | 38 | ❌ Not Started |
| Priority 3 (Review) | 2 | 23 | 97 | 🔍 Review Needed |
| Priority 3 (Good) | 7 | 87 | N/A | ✅ No Migration Needed |

## Migration Workflow

### For Each Test File

1. **Preparation**
   - Read the test file
   - Run tests to ensure they pass
   - Review MIGRATION_QUICK_REFERENCE.md

2. **Migration**
   - Add compilation verification (if not present)
   - Replace structural checks with semantic assertions
   - Preserve DynamoDB-specific checks with "because" messages
   - Update test documentation

3. **Validation**
   - Run all tests (should pass)
   - Test with formatting changes (should pass)
   - Test with intentional errors (should fail)
   - Verify error messages are clear

4. **Documentation**
   - Add file header comment
   - Update MIGRATION_STATUS.md
   - Check off task in tasks.md

## Test Infrastructure

### Available Tools

✅ **CompilationVerifier** - Verifies generated code compiles
- Location: `TestHelpers/CompilationVerifier.cs`
- Method: `AssertGeneratedCodeCompiles(string sourceCode, params string[] additionalSources)`
- Status: Ready to use

✅ **SemanticAssertions** - Verifies code structure semantically
- Location: `TestHelpers/SemanticAssertions.cs`
- Methods:
  - `ShouldContainMethod(string methodName, string because = "")`
  - `ShouldContainAssignment(string targetName, string because = "")`
  - `ShouldUseLinqMethod(string methodName, string because = "")`
  - `ShouldReferenceType(string typeName, string because = "")`
- Status: Ready to use

## Priority 1 Files (Start Here)

1. **MapperGeneratorTests.cs** (9 tests, 48 string assertions)
   - Core mapping logic
   - High complexity
   - Estimated: 3-4 hours

2. **AdvancedTypeGenerationTests.cs** (38 tests, 131 string assertions)
   - Advanced type handling
   - Very high complexity
   - Estimated: 8-10 hours

3. **KeysGeneratorTests.cs** (8 tests, 31 string assertions)
   - Key generation logic
   - Medium complexity
   - Estimated: 2-3 hours

## Success Criteria

### Quantitative
- [ ] All 65 tests in Priority 1 & 2 pass after migration
- [ ] String assertions reduced from 248 to ~70 (DynamoDB-specific only)
- [ ] Semantic assertions added: ~150
- [ ] Compilation verification added to 2 additional files
- [ ] Zero test failures after migration

### Qualitative
- [ ] Tests pass with intentional formatting changes
- [ ] Tests fail with intentional code errors
- [ ] Error messages are clear and actionable
- [ ] Code is more maintainable
- [ ] Documentation is updated

## Common Patterns

### Replace These

```csharp
// Method existence
result.Should().Contain("public static MethodName");
→ result.ShouldContainMethod("MethodName");

// Assignments
result.Should().Contain("entity.Id = ");
→ result.ShouldContainAssignment("entity.Id");

// LINQ usage
result.Should().Contain(".Select(");
→ result.ShouldUseLinqMethod("Select");

// Type references
result.Should().Contain("typeof(TestEntity)");
→ result.ShouldReferenceType("TestEntity");
```

### Keep These (with "because")

```csharp
// DynamoDB attribute types
result.Should().Contain("SS =", "should use String Set for HashSet<string>");

// Null handling
result.Should().Contain("!= null", "should check for null before adding to DynamoDB item");

// Key formats
result.Should().Contain("var keyValue = \"tenant#\" + id", "should use correct partition key format");
```

## Questions?

Refer to:
1. **MIGRATION_QUICK_REFERENCE.md** for patterns and examples
2. **design.md** for detailed design decisions
3. **MIGRATION_STATUS.md** for current progress
4. **BASELINE_METRICS.md** for detailed metrics

## Next Steps

1. ✅ Complete migration tracking infrastructure (Task 1)
2. ⏭️ Start with MapperGeneratorTests.cs (Task 2)
3. ⏭️ Continue with AdvancedTypeGenerationTests.cs (Task 3)
4. ⏭️ Continue with KeysGeneratorTests.cs (Task 4)
5. ⏭️ Move to Priority 2 files (Tasks 5-7)
6. ⏭️ Review Priority 3 files (Task 8)
7. ⏭️ Create final summary report (Task 9)
