// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Isles;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

class PriorityQueue
{
    private (int Element, float Priority)[] _nodes;
    private int[] _elementIndex;

    /// <summary>
    /// The number of nodes in the heap.
    /// </summary>
    private int _size;

    /// <summary>
    /// Specifies the arity of the d-ary heap, which here is quaternary.
    /// It is assumed that this value is a power of 2.
    /// </summary>
    private const int Arity = 4;

    /// <summary>
    /// The binary logarithm of <see cref="Arity" />.
    /// </summary>
    private const int Log2Arity = 2;

    public PriorityQueue()
    {
        _nodes = Array.Empty<(int, float)>();
        _elementIndex = Array.Empty<int>();
    }

    public int Count => _size;

    public void Enqueue(int element, float priority)
    {
        // Virtually add the node at the end of the underlying array.
        // Note that the node being enqueued does not need to be physically placed
        // there at this point, as such an assignment would be redundant.

        int currentSize = _size++;

        if (_nodes.Length == currentSize)
        {
            Grow(currentSize + 1);
        }

        if (element > _elementIndex.Length)
        {
            Array.Resize(ref _elementIndex, element);
        }

        MoveUp((element, priority), currentSize);
    }

    public bool TryDequeue([MaybeNullWhen(false)] out int element, [MaybeNullWhen(false)] out float priority)
    {
        if (_size != 0)
        {
            (element, priority) = _nodes[0];
            RemoveRootNode();
            return true;
        }

        element = default;
        priority = default;
        return false;
    }

    public void Fill(int count, float priority)
    {
        Debug.Assert(count > 0);

        if (_nodes.Length < _size + count)
        {
            Grow(_size + count);
        }

        if (count > _elementIndex.Length)
        {
            Array.Resize(ref _elementIndex, count);
        }

        (int, float)[] nodes = _nodes;
        for (int i = 0; i < count; i++)
        {
            nodes[i] = (i, priority);
            _elementIndex[i] = i;
        }

        _size = count;

        Heapify();
    }

    public void UpdatePriority(int element, float priority)
    {
        var nodeIndex = _elementIndex[element];
        if (nodeIndex < _size)
        {
            (int Element, float Priority) node = _nodes[nodeIndex];
            if (priority < node.Priority)
            {
                MoveUp((node.Element, priority), nodeIndex);
            }
            else if (priority > node.Priority)
            {
                MoveDown((node.Element, priority), nodeIndex);
            }
        }
    }

    private void Grow(int minCapacity)
    {
        Debug.Assert(_nodes.Length < minCapacity);

        const int GrowFactor = 2;
        const int MinimumGrow = 4;

        int newcapacity = GrowFactor * _nodes.Length;

        // Allow the queue to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _nodes.Length overflowed thanks to the (uint) cast
        if ((uint)newcapacity > Array.MaxLength) newcapacity = Array.MaxLength;

        // Ensure minimum growth is respected.
        newcapacity = Math.Max(newcapacity, _nodes.Length + MinimumGrow);

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < minCapacity) newcapacity = minCapacity;

        Array.Resize(ref _nodes, newcapacity);
    }

    /// <summary>
    /// Removes the node from the root of the heap
    /// </summary>
    private void RemoveRootNode()
    {
        int lastNodeIndex = --_size;

        if (lastNodeIndex > 0)
        {
            (int Element, float Priority) lastNode = _nodes[lastNodeIndex];
            MoveDown(lastNode, 0);
        }
    }

    /// <summary>
    /// Gets the index of an element's parent.
    /// </summary>
    private static int GetParentIndex(int index) => (index - 1) >> Log2Arity;

    /// <summary>
    /// Gets the index of the first child of an element.
    /// </summary>
    private static int GetFirstChildIndex(int index) => (index << Log2Arity) + 1;

    /// <summary>
    /// Converts an unordered list into a heap.
    /// </summary>
    private void Heapify()
    {
        // Leaves of the tree are in fact 1-element heaps, for which there
        // is no need to correct them. The heap property needs to be restored
        // only for higher nodes, starting from the first node that has children.
        // It is the parent of the very last element in the array.

        (int Element, float Priority)[] nodes = _nodes;
        int lastParentWithChildren = GetParentIndex(_size - 1);

        for (int index = lastParentWithChildren; index >= 0; --index)
        {
            MoveDown(nodes[index], index);
        }
    }

    /// <summary>
    /// Moves a node up in the tree to restore heap order.
    /// </summary>
    private void MoveUp((int Element, float Priority) node, int nodeIndex)
    {
        // Instead of swapping items all the way to the root, we will perform
        // a similar optimization as in the insertion sort.

        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        (int Element, float Priority)[] nodes = _nodes;
        int[] elementIndex = _elementIndex;

        while (nodeIndex > 0)
        {
            int parentIndex = GetParentIndex(nodeIndex);
            (int Element, float Priority) parent = nodes[parentIndex];

            if (node.Priority < parent.Priority)
            {
                nodes[nodeIndex] = parent;
                elementIndex[parent.Element] = nodeIndex;
                nodeIndex = parentIndex;
            }
            else
            {
                break;
            }
        }

        nodes[nodeIndex] = node;
        elementIndex[node.Element] = nodeIndex;
    }

    private void MoveDown((int Element, float Priority) node, int nodeIndex)
    {
        // The node to move down will not actually be swapped every time.
        // Rather, values on the affected path will be moved up, thus leaving a free spot
        // for this value to drop in. Similar optimization as in the insertion sort.

        Debug.Assert(0 <= nodeIndex && nodeIndex < _size);

        (int Element, float Priority)[] nodes = _nodes;
        int[] elementIndex = _elementIndex;
        int size = _size;

        int i;
        while ((i = GetFirstChildIndex(nodeIndex)) < size)
        {
            // Find the child node with the minimal priority
            (int Element, float Priority) minChild = nodes[i];
            int minChildIndex = i;

            int childIndexUpperBound = Math.Min(i + Arity, size);
            while (++i < childIndexUpperBound)
            {
                (int Element, float Priority) nextChild = nodes[i];
                if (nextChild.Priority < minChild.Priority)
                {
                    minChild = nextChild;
                    minChildIndex = i;
                }
            }

            // Heap property is satisfied; insert node in this location.
            if (node.Priority < minChild.Priority)
            {
                break;
            }

            // Move the minimal child up by one node and
            // continue recursively from its location.
            nodes[nodeIndex] = minChild;
            elementIndex[minChild.Element] = nodeIndex;
            nodeIndex = minChildIndex;
        }

        nodes[nodeIndex] = node;
        elementIndex[node.Element] = nodeIndex;
    }
}
