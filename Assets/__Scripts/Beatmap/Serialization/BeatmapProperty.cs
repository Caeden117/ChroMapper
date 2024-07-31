using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class BeatmapProperty : Attribute
{
    public readonly string JsonName;

    public BeatmapProperty(string jsonName) => JsonName = jsonName;
}
