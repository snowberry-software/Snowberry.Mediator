using System.Collections.Concurrent;

namespace Snowberry.Mediator.Tests.Common.Helper;

/// <summary>
/// Provides test isolation for static state in notification handlers.
/// Uses AsyncLocal to ensure each test execution context has its own isolated state.
/// </summary>
public static class TestIsolationContext
{
    private static readonly AsyncLocal<ConcurrentDictionary<string, object>> _asyncLocalContext = new();

    private static ConcurrentDictionary<string, object> Context =>
        _asyncLocalContext.Value ??= new ConcurrentDictionary<string, object>();

    /// <summary>
    /// Gets or creates an isolated ConcurrentBag for the specified key in the current test context.
    /// </summary>
    public static ConcurrentBag<T> GetOrCreateBag<T>(string key)
    {
        return (ConcurrentBag<T>)Context.GetOrAdd(key, _ => new ConcurrentBag<T>());
    }

    /// <summary>
    /// Gets or creates an isolated value for the specified key in the current test context.
    /// </summary>
    public static T GetOrCreateValue<T>(string key, Func<T> factory)
    {
        return (T)Context.GetOrAdd(key, _ => factory()!);
    }

    /// <summary>
    /// Gets or sets a value in the current test context.
    /// </summary>
    public static T GetOrSetValue<T>(string key, T value)
    {
        return (T)Context.AddOrUpdate(key, value!, (_, _) => value!);
    }

    /// <summary>
    /// Gets a value from the current test context, or returns the default value if not found.
    /// </summary>
    public static T GetValue<T>(string key, T defaultValue = default!)
    {
        return Context.TryGetValue(key, out object? value) ? (T)value : defaultValue;
    }

    /// <summary>
    /// Clears all state in the current test context.
    /// </summary>
    public static void Clear()
    {
        Context.Clear();
    }

    /// <summary>
    /// Initializes a new test context.
    /// </summary>
    public static void InitializeContext()
    {
        _asyncLocalContext.Value = new ConcurrentDictionary<string, object>();
    }
}