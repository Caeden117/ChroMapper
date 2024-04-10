using Newtonsoft.Json;
using System;
using UnityEngine;

public class ColorArrayConverter : JsonConverter<Color>
{    
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        
        writer.WriteValue(value.r);
        writer.WriteValue(value.g);
        writer.WriteValue(value.b);

        if (!Mathf.Approximately(value.a, 1))
        {
            writer.WriteValue(value.a);
        }
        
        writer.WriteEndArray();
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            throw new InvalidOperationException(
                "Wrong Color converter. You may be looking to use ColorObjectConverter instead.");
        }

        if (reader.TokenType != JsonToken.StartArray)
        {
            throw new InvalidOperationException("This is NOT a JSON array.");
        }

        var r = reader.ReadAsFloat();
        var g = reader.ReadAsFloat();
        var b = reader.ReadAsFloat();
        var a = reader.ReadAsFloat(false, 1.0f);

        if (reader.TokenType != JsonToken.EndArray) reader.Read();

        if (!hasExistingValue) return new Color(r, g, b, a);
        
        existingValue.Set(r, g, b, a);
        return existingValue;
    }
}
