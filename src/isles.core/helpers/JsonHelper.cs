// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Isles;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions Options = new();

    public static T? DeserializeAnonymousType<T>(byte[] utf8Json, T _, JsonSerializerOptions? options = default)
        => JsonSerializer.Deserialize<T>(utf8Json, options);

    static JsonHelper()
    {
        Options.Converters.Add(new PointJsonConverter());
        Options.Converters.Add(new Vector2JsonConverter());
        Options.Converters.Add(new Vector3JsonConverter());
    }

    private class PointJsonConverter : JsonConverter<Point>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Point);

        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            var x = reader.GetInt32();
            reader.Read();
            var y = reader.GetInt32();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }

            return new(x, y);
        }

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Vector2);

        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }

            return new(x, y);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Vector3);

        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();
            reader.Read();
            var z = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }

            return new(x, y, z);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}