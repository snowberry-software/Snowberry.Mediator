namespace Snowberry.Mediator.Abstractions;

/// <summary>
/// Empty <see langword="struct"/> to be used when no data is required or to represent a void type.
/// </summary>
public readonly struct Empty
{
    private static readonly Empty s_Value = new();

    /// <summary>
    /// Implicitly converts an <see cref="Empty"/> instance to a <see cref="ValueTask{TResult}"/> of <see cref="Empty"/>.
    /// </summary>
    /// <param name="_">The <see cref="Empty"/> instance (ignored).</param>
    public static implicit operator ValueTask<Empty>(Empty _)
    {
        return new(s_Value);
    }

    /// <summary>
    /// The singleton instance of <see cref="Empty"/>.
    /// </summary>
    public static ref readonly Empty Value => ref s_Value;

    /// <summary>
    /// The singleton <see cref="ValueTask{TResult}"/> instance of <see cref="Empty"/>.
    /// </summary>
    public static ValueTask<Empty> ValueTask => new(s_Value);
}