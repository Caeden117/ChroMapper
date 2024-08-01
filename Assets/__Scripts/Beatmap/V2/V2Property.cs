/// <summary>
/// An attribute specifying the JSON Property name within the v2 beatmap format.
/// </summary>
public sealed class V2Property : BaseBeatmapProperty
{
    public V2Property(string jsonName) : base(jsonName) { }
}
