using System;
using Newtonsoft.Json;

[JsonConverter(typeof(EventTypeJsonConverter))]
public readonly struct EnvironmentEventType
{
    public int Value { get; }

    public EnvironmentEventType(int value) => Value = value;

    public static implicit operator EnvironmentEventType(int value) => new(value);
    public static implicit operator int(EnvironmentEventType value) => value.Value;
}

public sealed class EventTypeJsonConverter : JsonConverter<EnvironmentEventType>
{
    public override EnvironmentEventType ReadJson(
        JsonReader reader,
        Type objectType,
        EnvironmentEventType existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var value = ReadString(reader);
        if (value.StartsWith("Event", StringComparison.Ordinal)
            && int.TryParse(value.Substring("Event".Length), out var eventType)
            && value == $"Event{eventType}"
            && eventType is >= 0 and <= 21)
            return eventType;

        return value switch
        {
            "VoidEvent" => -1,
            "Special0" => 40,
            "Special1" => 41,
            "Special2" => 42,
            "Special3" => 43,
            "BpmChange" => 100,
            _ => throw new JsonSerializationException($"Unknown event type '{value}'.")
        };
    }

    public override void WriteJson(JsonWriter writer, EnvironmentEventType value, JsonSerializer serializer)
    {
        var name = value.Value switch
        {
            >= 0 and <= 21 => $"Event{value.Value}",
            -1 => "VoidEvent",
            40 => "Special0",
            41 => "Special1",
            42 => "Special2",
            43 => "Special3",
            100 => "BpmChange",
            _ => throw new JsonSerializationException($"Unknown event type '{value.Value}'.")
        };
        writer.WriteValue(name);
    }

    private static string ReadString(JsonReader reader) =>
        reader.TokenType == JsonToken.String
            ? (string)reader.Value
            : throw new JsonSerializationException($"Expected an event type string, got {reader.TokenType}.");
}

[JsonConverter(typeof(BasicEventKindJsonConverter))]
public readonly struct EnvironmentBasicEventKind
{
    public BasicEventKind Value { get; }

    public EnvironmentBasicEventKind(BasicEventKind value) => Value = value;

    public static implicit operator EnvironmentBasicEventKind(BasicEventKind value) => new(value);
    public static implicit operator BasicEventKind(EnvironmentBasicEventKind value) => value.Value;
}

public sealed class BasicEventKindJsonConverter : JsonConverter<EnvironmentBasicEventKind>
{
    public override EnvironmentBasicEventKind ReadJson(
        JsonReader reader,
        Type objectType,
        EnvironmentBasicEventKind existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.String)
            throw new JsonSerializationException($"Expected a toolbar type string, got {reader.TokenType}.");

        return ((string)reader.Value) switch
        {
            "None" => BasicEventKind.None,
            "Lights" => BasicEventKind.Lights,
            "Toggle" => BasicEventKind.Toggle,
            "FloatValue" => BasicEventKind.FloatValue,
            "IntValue" => BasicEventKind.IntValue,
            "BtsCharacterSelection" => BasicEventKind.BtsCharacter,
            "CarSelection" => BasicEventKind.Car,
            var value => throw new JsonSerializationException($"Unknown toolbar type '{value}'.")
        };
    }

    public override void WriteJson(JsonWriter writer, EnvironmentBasicEventKind value, JsonSerializer serializer)
    {
        var name = value.Value switch
        {
            BasicEventKind.None => "None",
            BasicEventKind.Lights => "Lights",
            BasicEventKind.Toggle => "Toggle",
            BasicEventKind.FloatValue => "FloatValue",
            BasicEventKind.IntValue => "IntValue",
            BasicEventKind.BtsCharacter => "BtsCharacterSelection",
            BasicEventKind.Car => "CarSelection",
            _ => throw new JsonSerializationException($"Unknown toolbar type '{value.Value}'.")
        };
        writer.WriteValue(name);
    }
}

[JsonConverter(typeof(RotationStepTypeJsonConverter))]
public readonly struct EnvironmentRotationStepType
{
    public RotationStepType Value { get; }

    public EnvironmentRotationStepType(RotationStepType value) => Value = value;

    public static implicit operator EnvironmentRotationStepType(RotationStepType value) => new(value);
    public static implicit operator RotationStepType(EnvironmentRotationStepType value) => value.Value;
}

public sealed class RotationStepTypeJsonConverter : JsonConverter<EnvironmentRotationStepType>
{
    public override EnvironmentRotationStepType ReadJson(
        JsonReader reader,
        Type objectType,
        EnvironmentRotationStepType existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        return reader.TokenType == JsonToken.String
               && Enum.TryParse((string)reader.Value, out RotationStepType result)
            ? result
            : RotationStepType.Range;
    }

    public override void WriteJson(JsonWriter writer, EnvironmentRotationStepType value, JsonSerializer serializer) =>
        writer.WriteValue(value.Value.ToString());
}
