namespace Snowberry.Mediator.OpenTelemetry.Internals;

/// <summary>
/// Caches the display name used in span names and the <c>type</c> metric dimension for <typeparamref name="T"/>.
/// Uses the fully-qualified name so that types sharing a simple name across namespaces do not collide into the
/// same span name or metric time series. Computed once per closed type.
/// </summary>
/// <typeparam name="T">The request, response, or notification type.</typeparam>
internal static class TypeNameCache<T>
{
    /// <summary>The fully-qualified type name (falls back to the simple name for types without a full name).</summary>
    public static readonly string s_Name = typeof(T).FullName ?? typeof(T).Name;
}