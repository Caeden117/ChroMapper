using System;
using Newtonsoft.Json;
using UnityEngine;

public class ColorArrayConverter : JsonConverter<Color>
{
    public override Color ReadJson(
        JsonReader reader,
        Type objectType,
        Color existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray) throw new JsonSerializationException("Expected StartArray token");

        reader.Read();
        var r = Convert.ToSingle(reader.Value);

        reader.Read();
        var g = Convert.ToSingle(reader.Value);

        reader.Read();
        var b = Convert.ToSingle(reader.Value);

        reader.Read();
        var a = 1f;
        if (reader.TokenType is JsonToken.Float or JsonToken.Integer)
        {
            a = Convert.ToSingle(reader.Value);
            reader.Read();
        }

        return reader.TokenType != JsonToken.EndArray
            ? throw new JsonSerializationException("Expected EndArray token")
            : new Color(r, g, b, a);
    }

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.r);
        writer.WriteValue(value.g);
        writer.WriteValue(value.b);
        writer.WriteValue(value.a);
        writer.WriteEndArray();
    }
}
