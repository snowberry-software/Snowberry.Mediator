namespace Snowberry.Mediator.OpenTelemetry.Internals;

internal static class TypeNameCache<T>
{
    public static readonly string s_Name = typeof(T).Name;
}