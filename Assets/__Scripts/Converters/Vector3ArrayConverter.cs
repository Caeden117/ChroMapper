using System;
using Newtonsoft.Json;
using UnityEngine;

public class Vector3ArrayConverter : JsonConverter<Vector3>
{
    public override Vector3 ReadJson(
        JsonReader reader,
        Type objectType,
        Vector3 existingValue,
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
        return reader.TokenType != JsonToken.EndArray
            ? throw new JsonSerializationException("Expected EndArray token")
            : new Vector3(x, y, z);
    }

    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.x);
        writer.WriteValue(value.y);
        writer.WriteValue(value.z);
        writer.WriteEndArray();
    }
}
