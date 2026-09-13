using System;
using Newtonsoft.Json;
using UnityEngine;

public class Vector2ArrayConverter : JsonConverter<Vector2>
{
    public override Vector2 ReadJson(
        JsonReader reader,
        Type objectType,
        Vector2 existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Expected StartArray token");

        reader.Read();
        var x = Convert.ToSingle(reader.Value);

        reader.Read();
        var y = Convert.ToSingle(reader.Value);

        reader.Read();
        return reader.TokenType != JsonToken.EndArray
            ? throw new JsonSerializationException("Expected EndArray token")
            : new Vector2(x, y);
    }

    public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.x);
        writer.WriteValue(value.y);
        writer.WriteEndArray();
    }
}
