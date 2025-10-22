#!/bin/bash

# Unit Test Migration Analysis Script
# This script analyzes test files to identify string-based assertions and track migration progress

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

TEST_DIR="Oproto.FluentDynamoDb.SourceGenerator.UnitTests"

echo "========================================="
echo "Unit Test Migration Analysis"
echo "========================================="
echo ""

# Function to count tests in a file
count_tests() {
    local file=$1
    grep -c "^\s*\[Fact\]" "$file" 2>/dev/null || echo "0"
}

# Function to count string assertions
count_string_assertions() {
    local file=$1
    grep -c "Should()\.Contain(" "$file" 2>/dev/null || echo "0"
}

# Function to check for compilation verification
has_compilation_verification() {
    local file=$1
    if grep -q "CompilationVerifier\.AssertGeneratedCodeCompiles" "$file" 2>/dev/null; then
        echo "✅"
    else
        echo "❌"
    fi
}

# Function to check for semantic assertions
has_semantic_assertions() {
    local file=$1
    if grep -q "ShouldContainMethod\|ShouldContainAssignment\|ShouldUseLinqMethod\|ShouldReferenceType" "$file" 2>/dev/null; then
        echo "✅"
    else
        echo "❌"
    fi
}

echo "Analyzing test files..."
echo ""

# Priority 1 Files
echo -e "${BLUE}=== Priority 1: High Impact Tests ===${NC}"
echo ""

priority1_files=(
    "Generators/MapperGeneratorTests.cs"
    "Generators/AdvancedTypeGenerationTests.cs"
    "Generators/KeysGeneratorTests.cs"
)

total_p1_tests=0
total_p1_string_assertions=0

for file in "${priority1_files[@]}"; do
    filepath="$TEST_DIR/$file"
    if [ -f "$filepath" ]; then
        tests=$(count_tests "$filepath")
        string_assertions=$(count_string_assertions "$filepath")
        compilation_check=$(has_compilation_verification "$filepath")
        semantic_check=$(has_semantic_assertions "$filepath")
        
        total_p1_tests=$((total_p1_tests + tests))
        total_p1_string_assertions=$((total_p1_string_assertions + string_assertions))
        
        echo "📄 $file"
        echo "   Tests: $tests"
        echo "   String Assertions: $string_assertions"
        echo "   Compilation Verification: $compilation_check"
        echo "   Semantic Assertions: $semantic_check"
        echo ""
    fi
done

echo -e "${GREEN}Priority 1 Totals:${NC}"
echo "   Total Tests: $total_p1_tests"
echo "   Total String Assertions: $total_p1_string_assertions"
echo ""

# Priority 2 Files
echo -e "${BLUE}=== Priority 2: Medium Impact Tests ===${NC}"
echo ""

priority2_files=(
    "Generators/FieldsGeneratorTests.cs"
    "DynamoDbSourceGeneratorTests.cs"
    "Generators/MapperGeneratorBugFixTests.cs"
)

total_p2_tests=0
total_p2_string_assertions=0

for file in "${priority2_files[@]}"; do
    filepath="$TEST_DIR/$file"
    if [ -f "$filepath" ]; then
        tests=$(count_tests "$filepath")
        string_assertions=$(count_string_assertions "$filepath")
        compilation_check=$(has_compilation_verification "$filepath")
        semantic_check=$(has_semantic_assertions "$filepath")
        
        total_p2_tests=$((total_p2_tests + tests))
        total_p2_string_assertions=$((total_p2_string_assertions + string_assertions))
        
        echo "📄 $file"
        echo "   Tests: $tests"
        echo "   String Assertions: $string_assertions"
        echo "   Compilation Verification: $compilation_check"
        echo "   Semantic Assertions: $semantic_check"
        echo ""
    fi
done

echo -e "${GREEN}Priority 2 Totals:${NC}"
echo "   Total Tests: $total_p2_tests"
echo "   Total String Assertions: $total_p2_string_assertions"
echo ""

# Priority 3 Files (Review Needed)
echo -e "${BLUE}=== Priority 3: Files Needing Review ===${NC}"
echo ""

priority3_files=(
    "EdgeCases/EdgeCaseTests.cs"
    "Integration/EndToEndSourceGeneratorTests.cs"
)

total_p3_tests=0
total_p3_string_assertions=0

for file in "${priority3_files[@]}"; do
    filepath="$TEST_DIR/$file"
    if [ -f "$filepath" ]; then
        tests=$(count_tests "$filepath")
        string_assertions=$(count_string_assertions "$filepath")
        compilation_check=$(has_compilation_verification "$filepath")
        semantic_check=$(has_semantic_assertions "$filepath")
        
        total_p3_tests=$((total_p3_tests + tests))
        total_p3_string_assertions=$((total_p3_string_assertions + string_assertions))
        
        echo "📄 $file"
        echo "   Tests: $tests"
        echo "   String Assertions: $string_assertions"
        echo "   Compilation Verification: $compilation_check"
        echo "   Semantic Assertions: $semantic_check"
        echo ""
    fi
done

echo -e "${GREEN}Priority 3 Totals:${NC}"
echo "   Total Tests: $total_p3_tests"
echo "   Total String Assertions: $total_p3_string_assertions"
echo ""

# Overall Summary
echo "========================================="
echo -e "${YELLOW}Overall Summary${NC}"
echo "========================================="
echo ""
echo "Priority 1 (High Impact):"
echo "   Files: ${#priority1_files[@]}"
echo "   Tests: $total_p1_tests"
echo "   String Assertions: $total_p1_string_assertions"
echo ""
echo "Priority 2 (Medium Impact):"
echo "   Files: ${#priority2_files[@]}"
echo "   Tests: $total_p2_tests"
echo "   String Assertions: $total_p2_string_assertions"
echo ""
echo "Priority 3 (Review Needed):"
echo "   Files: ${#priority3_files[@]}"
echo "   Tests: $total_p3_tests"
echo "   String Assertions: $total_p3_string_assertions"
echo ""
echo "Total to Migrate:"
echo "   Files: $((${#priority1_files[@]} + ${#priority2_files[@]}))"
echo "   Tests: $((total_p1_tests + total_p2_tests))"
echo "   String Assertions: $((total_p1_string_assertions + total_p2_string_assertions))"
echo ""

# Check for specific patterns
echo "========================================="
echo "Pattern Analysis"
echo "========================================="
echo ""

echo "Searching for common patterns in Priority 1 & 2 files..."
echo ""

all_files=("${priority1_files[@]}" "${priority2_files[@]}")

for pattern in "Should\(\)\.Contain\(\"public static" "Should\(\)\.Contain\(\"entity\." "Should\(\)\.Contain\(\"\.Select\(" "Should\(\)\.Contain\(\"typeof\(" "Should\(\)\.Contain\(\"S =\"" "Should\(\)\.Contain\(\"N =\"" "Should\(\)\.Contain\(\"SS =\"" "Should\(\)\.Contain\(\"NS =\"" "Should\(\)\.Contain\(\"L =\"" "Should\(\)\.Contain\(\"M =\""; do
    count=0
    for file in "${all_files[@]}"; do
        filepath="$TEST_DIR/$file"
        if [ -f "$filepath" ]; then
            file_count=$(grep -c "$pattern" "$filepath" 2>/dev/null || echo "0")
            count=$((count + file_count))
        fi
    done
    if [ $count -gt 0 ]; then
        echo "   $pattern: $count occurrences"
    fi
done

echo ""
echo "========================================="
echo "Analysis Complete"
echo "========================================="
