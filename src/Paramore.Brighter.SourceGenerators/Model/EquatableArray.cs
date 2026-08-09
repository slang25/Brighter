#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Paramore.Brighter.SourceGenerators.Model;

/// <summary>
/// Value-equatable wrapper around an array, suitable for use in records that participate in
/// the incremental generator pipeline (where structural equality is required for caching).
/// </summary>
internal sealed class EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

    private readonly T[] _items;
    private int _hashCode;

    public EquatableArray(IEnumerable<T> items) => _items = items.ToArray();

    public int Count => _items.Length;

    public T this[int index] => _items[index];

    public bool Equals(EquatableArray<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_items.Length != other._items.Length) return false;
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < _items.Length; i++)
        {
            if (!comparer.Equals(_items[i], other._items[i])) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <summary>
    /// Hashes every element, so it is cached after the first call: the incremental pipeline hashes
    /// compilation-wide arrays repeatedly while comparing cache keys, and the contents are immutable
    /// once constructed. Zero is the "not computed yet" sentinel, so a fold that happens to land on
    /// zero is stored as one — a collision with an unrelated array, not a correctness problem, since
    /// equality is decided by <see cref="Equals(EquatableArray{T}?)"/>. Deliberately not
    /// thread-guarded: a race recomputes the same value.
    /// </summary>
    public override int GetHashCode()
    {
        if (_hashCode != 0)
            return _hashCode;

        unchecked
        {
            var comparer = EqualityComparer<T>.Default;
            var hash = 17;
            foreach (var item in _items)
                hash = hash * 31 + (item is null ? 0 : comparer.GetHashCode(item));
            _hashCode = hash == 0 ? 1 : hash;
            return _hashCode;
        }
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
