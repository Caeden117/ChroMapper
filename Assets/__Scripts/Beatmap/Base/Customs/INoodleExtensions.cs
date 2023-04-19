using UnityEngine;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public interface INoodleExtensionsGrid
    {
        Vector2? CustomCoordinate { get; set; }
        JSONNode CustomWorldRotation { get; set; }
        JSONNode CustomLocalRotation { get; set; }

        string CustomKeyCoordinate { get; }
        string CustomKeyWorldRotation { get; }
        string CustomKeyLocalRotation { get; }
    }

    public interface INoodleExtensionsNote : INoodleExtensionsGrid
    {
        float? CustomDirection { get; set; }

        string CustomKeyDirection { get; }
    }

    public interface INoodleExtensionsSlider : INoodleExtensionsGrid
    {
        Vector2? CustomTailCoordinate { get; set; }

        string CustomKeyTailCoordinate { get; }
    }

    public interface INoodleExtensionsObstacle : INoodleExtensionsGrid
    {
        JSONNode CustomSize { get; set; }

        string CustomKeySize { get; }
    }

    public interface INoodleExtensionsEvent
    {
        float? CustomLaneRotation { get; set; }

        string CustomKeyLaneRotation { get; }
    }
}
