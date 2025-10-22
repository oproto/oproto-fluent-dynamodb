# Conditional Compilation Example

This example demonstrates how to configure your project to disable DynamoDB logging using conditional compilation.

## Overview

All logging code in Oproto.FluentDynamoDb is wrapped in `#if !DISABLE_DYNAMODB_LOGGING` directives. When you define the `DISABLE_DYNAMODB_LOGGING` symbol, the C# compiler completely removes all logging code from the compiled assembly, resulting in:

- **Zero runtime overhead** - No logging calls, no parameter evaluation
- **Smaller binary size** - Logging code is not included
- **No performance impact** - Code runs as if logging never existed

## Configuration Options

### Option 1: Disable in Release Builds Only (Recommended)

This is the most common approach - keep logging enabled for debugging in Debug builds, but disable it in Release builds for production:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DefineConstants>$(DefineConstants);DISABLE_DYNAMODB_LOGGING</DefineConstants>
</PropertyGroup>
```

**Build commands:**
```bash
# Debug build - logging enabled
dotnet build -c Debug

# Release build - logging disabled
dotnet build -c Release
```

### Option 2: Disable in All Configurations

To disable logging everywhere:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DISABLE_DYNAMODB_LOGGING</DefineConstants>
</PropertyGroup>
```

### Option 3: Custom Configurations

Create custom build configurations for different environments:

```xml
<!-- Development: Full logging -->
<PropertyGroup Condition="'$(Configuration)' == 'Development'">
  <!-- Logging enabled by default -->
</PropertyGroup>

<!-- Staging: Full logging -->
<PropertyGroup Condition="'$(Configuration)' == 'Staging'">
  <!-- Logging enabled by default -->
</PropertyGroup>

<!-- Production: No logging -->
<PropertyGroup Condition="'$(Configuration)' == 'Production'">
  <DefineConstants>$(DefineConstants);DISABLE_DYNAMODB_LOGGING</DefineConstants>
</PropertyGroup>
```

**Build commands:**
```bash
dotnet build -c Development
dotnet build -c Staging
dotnet build -c Production
```

## Verification

### Verify Logging is Disabled

1. Build with the symbol defined:
   ```bash
   dotnet build -c Release
   ```

2. Inspect the generated code in `obj/Release/net8.0/generated/` - you'll see the `#if !DISABLE_DYNAMODB_LOGGING` directives but the code between them is excluded by the compiler.

3. Use a decompiler (ILSpy, dotPeek) to inspect the compiled assembly - logging-related strings should not exist.

### Verify Logging is Enabled

1. Build without the symbol:
   ```bash
   dotnet build -c Debug
   ```

2. Run your application with a logger configured and verify log output appears.

## Performance Impact

### With Logging Enabled (Debug)

When using `NoOpLogger` (the default):
- Minimal overhead from null-conditional operators
- No allocations when logger is null
- Typically < 1% performance impact

### With Logging Disabled (Release)

When `DISABLE_DYNAMODB_LOGGING` is defined:
- Zero overhead - all logging code removed
- No allocations
- Identical performance to code without logging

## Best Practices

1. **Use Configuration-Based Disabling**: Tie logging to build configuration rather than manually editing code.

2. **Keep Logging Enabled During Development**: Logging is invaluable for debugging, especially in AOT scenarios.

3. **Test Both Configurations**: Ensure your application works correctly with both logging enabled and disabled.

4. **Document Your Configuration**: Add comments explaining why logging is disabled in certain configurations.

## Example Usage

See `Example.csproj` in this directory for a complete working example with all three configuration options.

## Related Documentation

- [Conditional Compilation for Logging](../../docs/advanced-topics/conditional-compilation-logging.md) - Comprehensive guide
- [Logging Configuration](../../docs/advanced-topics/logging-configuration.md) - How to configure logging when enabled
- [Performance Optimization](../../docs/advanced-topics/performance-optimization.md) - Other performance tuning options
