/// <summary>
/// An attribute specifying the JSON Property name within the v3 beatmap format.
/// </summary>
public sealed class V3Property : BaseBeatmapProperty
{
    public V3Property(string jsonName) : base(jsonName) { }
}
