// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Generic
{
    public static class CollectionHelper
    {
        public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V val)
        {
            key = pair.Key;
            val = pair.Value;
        }
    }
}
