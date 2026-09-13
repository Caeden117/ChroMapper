using System;
using Newtonsoft.Json;
using UnityEngine;

public class Vector4ArrayConverter : JsonConverter<Vector4>
{
    public override Vector4 ReadJson(
        JsonReader reader,
        Type objectType,
        Vector4 existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Expected StartArray token");

        reader.Read();
        var x = Convert.ToSingle(reader.Value);

        reader.Read();
        var y = Convert.ToSingle(reader.Value);

        reader.Read();
        var z = Convert.ToSingle(reader.Value);

        reader.Read();
        var w = Convert.ToSingle(reader.Value);

        reader.Read();
        return reader.TokenType != JsonToken.EndArray
            ? throw new JsonSerializationException("Expected EndArray token")
            : new Vector4(x, y, z, w);
    }

    public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.x);
        writer.WriteValue(value.y);
        writer.WriteValue(value.z);
        writer.WriteValue(value.w);
        writer.WriteEndArray();
    }
}
