using UnityEngine;

namespace Beatmap.Base.Customs
{
    public interface INoodleExtensionsGrid
    {
        Vector2? CustomCoordinate { get; set; }
        Vector3? CustomWorldRotation { get; set; }
        Vector3? CustomLocalRotation { get; set; }

        string CustomKeyCoordinate { get; }
        string CustomKeyWorldRotation { get; }
        string CustomKeyLocalRotation { get; }
    }

    public interface INoodleExtensionsNote : INoodleExtensionsGrid
    {
        int? CustomDirection { get; set; }

        string CustomKeyDirection { get; }
    }

    public interface INoodleExtensionsObstacle : INoodleExtensionsGrid
    {
        Vector3? CustomSize { get; set; }

        string CustomKeySize { get; }
    }

    public interface INoodleExtensionsEvent
    {
        int? CustomLaneRotation { get; set; }

        string CustomKeyLaneRotation { get; }
    }
}
