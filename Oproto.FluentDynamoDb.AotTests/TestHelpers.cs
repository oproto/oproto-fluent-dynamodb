using System;

namespace Oproto.FluentDynamoDb.AotTests;

/// <summary>
/// Helper methods for AOT test assertions.
/// </summary>
public static class TestHelpers
{
    public static bool AssertEqual<T>(string testName, T expected, T actual)
    {
        if (Equals(expected, actual))
        {
            Console.WriteLine($"  ✓ {testName}");
            return true;
        }
        else
        {
            Console.WriteLine($"  ✗ {testName}");
            Console.WriteLine($"    Expected: {expected}");
            Console.WriteLine($"    Actual:   {actual}");
            return false;
        }
    }

    public static bool AssertTrue(string testName, bool condition)
    {
        if (condition)
        {
            Console.WriteLine($"  ✓ {testName}");
            return true;
        }
        else
        {
            Console.WriteLine($"  ✗ {testName}");
            return false;
        }
    }

    public static bool AssertThrows<TException>(string testName, Action action) where TException : Exception
    {
        try
        {
            action();
            Console.WriteLine($"  ✗ {testName} - Expected exception {typeof(TException).Name} but none was thrown");
            return false;
        }
        catch (TException)
        {
            Console.WriteLine($"  ✓ {testName}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ {testName} - Expected {typeof(TException).Name} but got {ex.GetType().Name}");
            return false;
        }
    }

    public static bool AssertContains(string testName, string haystack, string needle)
    {
        if (haystack.Contains(needle))
        {
            Console.WriteLine($"  ✓ {testName}");
            return true;
        }
        else
        {
            Console.WriteLine($"  ✗ {testName}");
            Console.WriteLine($"    Expected to contain: {needle}");
            Console.WriteLine($"    Actual string: {haystack}");
            return false;
        }
    }
}
