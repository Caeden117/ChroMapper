using Newtonsoft.Json;
using System;
using UnityEngine;

/// <summary>
/// De/serializes a <see cref="Color"/> in an Object format, with each color channel being given a 
/// </summary>
public class ColorObjectCoverter : JsonConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        
        writer.WritePropertyName(nameof(value.r));
        writer.WriteValue(value.r);
        
        writer.WritePropertyName(nameof(value.g));
        writer.WriteValue(value.g);
        
        writer.WritePropertyName(nameof(value.b));
        writer.WriteValue(value.b);

        if (!Mathf.Approximately(value.a, 1))
        {
            writer.WritePropertyName(nameof(value.a));
            writer.WriteValue(value.a);
        }
        
        writer.WriteEndObject();
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartArray)
        {
            throw new InvalidOperationException(
                "Wrong Color converter. You may be looking to use ColorArrayConverter instead.");
        }

        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new InvalidOperationException("This is NOT a JSON object.");
        }

        reader.Read();

        var color = hasExistingValue ? existingValue : new Color();
        
        ReadColorProperty(reader, ref color);
        ReadColorProperty(reader, ref color);
        ReadColorProperty(reader, ref color);

        if (reader.TokenType == JsonToken.PropertyName)
        {
            ReadColorProperty(reader, ref color);
        }
        else
        {
            color.a = 1;
        }
        
        return color;
    }

    private void ReadColorProperty(JsonReader reader, ref Color color)
    {
        var propertyName = (string)reader.Value;

        switch (propertyName)
        {
            case "r":
                color.r = reader.ReadAsFloat();
                break;
            case "g":
                color.g = reader.ReadAsFloat();
                break;
            case "b":
                color.b = reader.ReadAsFloat();
                break;
            case "a":
                color.a = reader.ReadAsFloat();
                break;
            default:
                throw new InvalidOperationException($"Unsupported property \"{propertyName}\".");
        }

        reader.Read();
    }
}
