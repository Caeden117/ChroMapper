using System;

/// <summary>
/// An attribute specifying the JSON Property name for a specific beatmap version.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public abstract class BaseBeatmapProperty : Attribute
{
    public readonly string JsonName;

    public BaseBeatmapProperty(string jsonName)
    {
        JsonName = jsonName;
    }
}
