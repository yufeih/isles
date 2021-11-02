// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Generic
{
    public class LruCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<(TKey key, TValue value)>> _cache = new();
        private readonly LinkedList<(TKey key, TValue value)> _lruList = new();

        public LruCache(int capacity) => _capacity = capacity;

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _lruList.AddLast(node);
                return node.Value.value;
            }

            if (_cache.Count >= _capacity)
            {
                node = _lruList.First;
                _lruList.RemoveFirst();
                _cache.Remove(node.Value.key);
            }

            var value = valueFactory(key);
            _lruList.AddLast(_cache[key] = new((key, value)));
            return value;
        }
    }
}
