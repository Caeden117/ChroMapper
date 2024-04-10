using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleJSON;
using UnityEngine;

public class SimpleJSONConverter : JsonConverter<JSONNode>
{
    public override void WriteJson(JsonWriter writer, JSONNode value, JsonSerializer serializer)
        => writer.WriteRaw(value.ToString());

    public override JSONNode ReadJson(JsonReader reader, Type objectType, JSONNode existingValue, bool hasExistingValue, JsonSerializer serializer)
        => JSONNode.Parse(JToken.Load(reader).ToString());
}
