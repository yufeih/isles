// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Isles;

public class TypeDiscriminatorJsonConverter<T> : JsonConverter<T>
{
    private readonly Dictionary<string, Type> _knownTypes;

    public TypeDiscriminatorJsonConverter(Type[] knownTypes)
    {
        _knownTypes = knownTypes.ToDictionary(type => type.Name);
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        if (!jsonDocument.RootElement.TryGetProperty("Type", out var typeProperty))
        {
            throw new JsonException();
        }

        var type = _knownTypes[typeProperty.GetString() ?? ""];
        if (type == null)
        {
            throw new JsonException();
        }

        return (T?)JsonSerializer.Deserialize(jsonDocument, type, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object?)value, options);
    }
}
