// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Text.Json
{
    public static class JsonHelper
    {
        public static T DeserializeAnonymousType<T>(byte[] utf8Json, T _, JsonSerializerOptions options = default)
            => JsonSerializer.Deserialize<T>(utf8Json, options);
    }
}
