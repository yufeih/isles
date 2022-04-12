// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public struct ArrayBuilder<T> where T : struct
{
    private T[] _items;
    private int _count;

    public int Length => _count;

    public ref T this[int index] => ref _items[index];

    public ReadOnlySpan<T> AsSpan()
    {
        return _items.AsSpan(0, _count);
    }

    public void Clear()
    {
        _count = 0;
    }

    public void Add(T item)
    {
        if (_items is null || _count == _items.Length)
        {
            Array.Resize(ref _items, 2 * _count + 1);
        }

        _items[_count++] = item;
    }

    public void AddRange(ReadOnlySpan<T> newItems)
    {
        if (newItems.Length == 0)
        {
            return;
        }

        EnsureCapacity(_count + newItems.Length);
        newItems.CopyTo(_items.AsSpan(_count));
        _count += newItems.Length;
    }

    public void EnsureCapacity(int requestedCapacity)
    {
        if (requestedCapacity > (_items != null ? _items.Length : 0))
        {
            var newCount = Math.Max(2 * _count + 1, requestedCapacity);
            Array.Resize(ref _items, newCount);
        }
    }

    public ReadOnlySpan<T> ConvertAll<U>(ReadOnlySpan<U> items, Func<U, T> convert)
    {
        if (_items is null || _items.Length < items.Length)
        {
            Array.Resize(ref _items, items.Length);
        }

        _count = 0;
        foreach (var item in items)
        {
            _items[_count++] = convert(item);
        }

        return _items.AsSpan(0, _count);
    }

    public static implicit operator ReadOnlySpan<T>(ArrayBuilder<T> builder)
    {
        return builder._items.AsSpan(0, builder._count);
    }
}
