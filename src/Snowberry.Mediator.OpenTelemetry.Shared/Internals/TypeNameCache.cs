namespace Snowberry.Mediator.OpenTelemetry.Internals;

internal static class TypeNameCache<T>
{
    public static readonly string Name = typeof(T).Name;
}
