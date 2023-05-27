using UnityEngine;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public interface INoodleExtensionsGrid
    {
        JSONNode CustomAnimation { get; set; }
        JSONNode CustomCoordinate { get; set; }
        JSONNode CustomWorldRotation { get; set; }
        JSONNode CustomLocalRotation { get; set; }
        JSONNode CustomNoteJumpMovementSpeed { get; set; }
        JSONNode CustomNoteJumpStartBeatOffset { get; set; }


        string CustomKeyAnimation { get; }
        string CustomKeyCoordinate { get; }
        string CustomKeyWorldRotation { get; }
        string CustomKeyLocalRotation { get; }
        string CustomKeyNoteJumpMovementSpeed { get; }
        string CustomKeyNoteJumpStartBeatOffset { get; }
    }

    public interface INoodleExtensionsNote : INoodleExtensionsGrid
    {
        float? CustomDirection { get; set; }

        string CustomKeyDirection { get; }
    }

    public interface INoodleExtensionsSlider : INoodleExtensionsGrid
    {
        JSONNode CustomTailCoordinate { get; set; }

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
