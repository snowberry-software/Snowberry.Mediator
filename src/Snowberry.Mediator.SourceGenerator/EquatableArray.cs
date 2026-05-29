using System;
using System.Collections;
using System.Collections.Generic;

namespace Snowberry.Mediator.SourceGenerator;

/// <summary>
/// A small immutable array wrapper with structural (value) equality, so it can be used inside the
/// incremental generator's cached models without defeating Roslyn's caching (unlike
/// <see cref="System.Collections.Immutable.ImmutableArray{T}"/>, which has reference equality).
/// </summary>
/// <typeparam name="T">The element type, which must itself provide value equality.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    /// <summary>An empty array instance.</summary>
    public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

    private readonly T[]? _array;

    /// <summary>Initializes a new instance wrapping the given backing array.</summary>
    /// <param name="array">The backing array.</param>
    public EquatableArray(T[] array) => _array = array;

    /// <summary>Gets the number of elements.</summary>
    public int Count => _array?.Length ?? 0;

    /// <summary>Gets the element at the specified index.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The element at <paramref name="index"/>.</returns>
    public T this[int index] => _array![index];

    /// <summary>Determines whether this array is element-wise equal to <paramref name="other"/>.</summary>
    /// <param name="other">The array to compare against.</param>
    /// <returns><see langword="true"/> if the arrays are equal; otherwise <see langword="false"/>.</returns>
    public bool Equals(EquatableArray<T> other)
    {
        var a = _array ?? Array.Empty<T>();
        var b = other._array ?? Array.Empty<T>();

        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (!a[i].Equals(b[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (_array is null)
            return 0;

        unchecked
        {
            int hash = 17;
            for (int i = 0; i < _array.Length; i++)
                hash = (hash * 31) + _array[i].GetHashCode();

            return hash;
        }
    }

    /// <summary>Returns an enumerator over the elements.</summary>
    /// <returns>An enumerator over the elements.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        var array = _array ?? Array.Empty<T>();
        for (int i = 0; i < array.Length; i++)
            yield return array[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Creates an <see cref="EquatableArray{T}"/> from a list, returning <see cref="Empty"/> when empty.</summary>
    /// <param name="items">The source items.</param>
    /// <returns>The equatable array.</returns>
    public static EquatableArray<T> From(List<T> items) => items.Count == 0 ? Empty : new(items.ToArray());
}
