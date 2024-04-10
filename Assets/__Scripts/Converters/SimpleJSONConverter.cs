using System;
using System.Text;
using Newtonsoft.Json;
using SimpleJSON;
using UnityEngine;

public class SimpleJSONConverter : JsonConverter<JSONNode>
{
    public override void WriteJson(JsonWriter writer, JSONNode value, JsonSerializer serializer)
        => writer.WriteRaw(value.ToString());

    public override JSONNode ReadJson(JsonReader reader, Type objectType, JSONNode existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var stringBuilder = new StringBuilder();

        ReadStep(reader, stringBuilder);

        while (reader.Read())
        {
            ReadStep(reader, stringBuilder);
            
            if (reader.TokenType is not JsonToken.EndObject and not JsonToken.EndArray and not JsonToken.PropertyName)
            {
                stringBuilder.Append(",");
            }
        }
        
        return JSONNode.Parse(stringBuilder.ToString());
    }

    private void ReadStep(JsonReader reader, StringBuilder stringBuilder)
    {
        var token = reader.TokenType switch
        {
            JsonToken.StartArray => "[",
            JsonToken.StartObject => "{",
            
            JsonToken.EndObject => "}",
            JsonToken.EndArray => "]",
            
            JsonToken.Null => "null",
            JsonToken.Undefined => "undefined",
            
            JsonToken.PropertyName => $"\"{reader.Value}\":",
            JsonToken.String => $"\"{reader.Value}\"",
            _ => reader.Value
        };

        stringBuilder.Append(token);
    }
}
