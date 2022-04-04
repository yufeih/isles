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
public class IndexedPriorityQueue
{
    /// <summary>
    /// Internal queue elements.
    /// The first element is not used for easy index generation.
    /// </summary>
    private readonly int[] data;

    /// <summary>
    /// Keep track of the position of individual item in the heap.
    /// E.g. index[3] = 5 means that data[5] = 3.
    /// </summary>
    private readonly int[] index;

    /// <summary>
    /// Cost of each item.
    /// </summary>
    private readonly float[] costs;

    /// <summary>
    /// Actual data length.
    /// </summary>
    private int count;

    /// <summary>
    /// Gets element index array.
    /// </summary>
    public int[] Index => index;

    /// <summary>
    /// Gets priority queue element count.
    /// </summary>
    public int Count => count;

    /// <summary>
    /// Gets priority queue capacity.
    /// </summary>
    public int Capacity => data.Length;

    /// <summary>
    /// Gets whether the queue is empty.
    /// </summary>
    public bool Empty => count == 0;

    /// <summary>
    /// Retrive the minimun (top) element without removing it.
    /// </summary>
    public int Top => data[1];

    /// <summary>
    /// Creates a priority queue to hold n elements.
    /// </summary>
    /// <param name="capacity"></param>
    public IndexedPriorityQueue(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        data = new int[capacity];
        costs = new float[capacity];
        index = new int[capacity];

        Clear();
    }

    /// <summary>
    /// Clear the priority queue.
    /// </summary>
    public void Clear()
    {
        for (var i = 0; i < costs.Length; i++)
        {
            costs[i] = 0;
            index[i] = -1;
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
            if (x > 0 && cost < costs[x])
            {
                costs[i] = costs[x];
                data[i] = data[x];
                index[data[x]] = i;
                i = x;
            }
            else
            {
                break;
            }
        }

        // Assign the new element
        costs[i] = cost;
        data[i] = element;
        index[element] = i;
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
        var top = data[1];
        index[top] = 0;
        FixHeap(1, count - 1, data[count], costs[count]);
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
        i = index[element];
        if (i <= 0)
        {
            return;
        }

        // Bubble up the heap
        while (i > 0)
        {
            x = i >> 1;
            if (x > 0 && cost < costs[x])
            {
                costs[i] = costs[x];
                data[i] = data[x];
                index[data[x]] = i;
                i = x;
            }
            else
            {
                break;
            }
        }

        // Assign the new element
        costs[i] = cost;
        data[i] = element;
        index[element] = i;
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
                min = x == n ? x : (costs[x] < costs[x + 1]) ? x : x + 1;
            }

            if (costs[min] < cost)
            {
                costs[i] = costs[min];
                data[i] = data[min];  /* Sink if k is bigger */
                index[data[min]] = i;
                i = min;
            }
            else
            {
                break;          /* Otherwise fix is done */
            }
        }

        costs[i] = cost;
        data[i] = k;
        index[k] = i;
    }
}
