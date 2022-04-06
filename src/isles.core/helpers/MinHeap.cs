// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

/// <summary>
/// Use min heap to implement a priority queue.
/// Used to implement Dijkstra's algorithm.
/// </summary>
/// <remarks>
/// The size of the indexed priority queue is fixed.
/// </remarks>
internal class MinHeap
{
    /// <summary>
    /// Internal queue elements.
    /// The first element is not used for easy index generation.
    /// </summary>
    private readonly int[] _data;

    /// <summary>
    /// Keep track of the position of individual item in the heap.
    /// E.g. index[3] = 5 means that data[5] = 3.
    /// </summary>
    private readonly int[] _index;

    /// <summary>
    /// Cost of each item.
    /// </summary>
    private readonly float[] _costs;

    /// <summary>
    /// Actual data length.
    /// </summary>
    private int count;

    /// <summary>
    /// Gets element index array.
    /// </summary>
    public int[] Index => _index;

    /// <summary>
    /// Gets whether the queue is empty.
    /// </summary>
    public bool Empty => count == 0;

    /// <summary>
    /// Creates a priority queue to hold n elements.
    /// </summary>
    /// <param name="capacity"></param>
    public MinHeap(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        _data = new int[capacity];
        _costs = new float[capacity];
        _index = new int[capacity];

        Clear();
    }

    /// <summary>
    /// Clear the priority queue.
    /// </summary>
    public void Clear()
    {
        for (var i = 0; i < _costs.Length; i++)
        {
            _costs[i] = 0;
            _index[i] = -1;
        }

        count = 0;
    }

    /// <summary>
    /// Adds an element to the queue.
    /// </summary>
    public void Add(int element, float cost)
    {
        int i, x;

        // Bubble up the heap
        i = ++count;
        while (i > 0)
        {
            x = i >> 1;
            if (x > 0 && cost < _costs[x])
            {
                _costs[i] = _costs[x];
                _data[i] = _data[x];
                _index[_data[x]] = i;
                i = x;
            }
            else
            {
                break;
            }
        }

        // Assign the new element
        _costs[i] = cost;
        _data[i] = element;
        _index[element] = i;
    }

    /// <summary>
    /// Remove and retrieve the minimun (top) element.
    /// </summary>
    public int Pop()
    {
        if (count <= 0)
        {
            throw new InvalidOperationException();
        }

        // Make use of the first element here
        var top = _data[1];
        _index[top] = 0;
        FixHeap(1, count - 1, _data[count], _costs[count]);
        count--;
        return top;
    }

    /// <summary>
    /// Increase the priority of a given node.
    /// </summary>
    public void IncreasePriority(int element, float cost)
    {
        int x, i;

        // Check to see if the element is in the heap
        i = _index[element];
        if (i <= 0)
        {
            return;
        }

        // Bubble up the heap
        while (i > 0)
        {
            x = i >> 1;
            if (x > 0 && cost < _costs[x])
            {
                _costs[i] = _costs[x];
                _data[i] = _data[x];
                _index[_data[x]] = i;
                i = x;
            }
            else
            {
                break;
            }
        }

        // Assign the new element
        _costs[i] = cost;
        _data[i] = element;
        _index[element] = i;
    }

    /// <summary>
    /// Fix the heap.
    /// </summary>
    /// <param name="elements">Array of elements to be fixed.</param>
    /// <param name="i">Root index of the subtree.</param>
    /// <param name="n">Subtree size.</param>
    /// <param name="k">Element to be add as the root.</param>
    private void FixHeap(int i, int n, int k, float cost)
    {
        int x, min;
        while (i <= n)
        {
            x = i << 1;         /* Left subtree */
            if (x > n)
            {
                break;
            }
            else
            {
                min = x == n ? x : (_costs[x] < _costs[x + 1]) ? x : x + 1;
            }

            if (_costs[min] < cost)
            {
                _costs[i] = _costs[min];
                _data[i] = _data[min];  /* Sink if k is bigger */
                _index[_data[min]] = i;
                i = min;
            }
            else
            {
                break;          /* Otherwise fix is done */
            }
        }

        _costs[i] = cost;
        _data[i] = k;
        _index[k] = i;
    }
}
