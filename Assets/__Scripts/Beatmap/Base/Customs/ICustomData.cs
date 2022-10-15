using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public interface ICustomData
    {
        JSONNode CustomData { get; set; }

        bool IsChroma();
        bool IsNoodleExtensions();
        bool IsMappingExtensions();
    }

    public interface ICustomDataDifficulty : ICustomData
    {
        float Time { get; set; }
        List<BaseBpmChange> BpmChanges { get; set; }
        List<BaseBookmark> Bookmarks { get; set; }
        List<BaseCustomEvent> CustomEvents { get; set; }
        List<BaseEnvironmentEnhancement> EnvironmentEnhancements { get; set; }
    }

    public interface ICustomDataNote : ICustomData, IChromaObject, INoodleExtensionsNote
    {
    }

    public interface ICustomDataBomb : ICustomData, IChromaObject, INoodleExtensionsGrid
    {
    }

    public interface ICustomDataSlider : ICustomData, IChromaObject, INoodleExtensionsSlider
    {
    }

    public interface ICustomDataArc : ICustomDataSlider
    {
    }

    public interface ICustomDataChain : ICustomDataSlider
    {
    }

    public interface ICustomDataObstacle : ICustomData, IChromaObject, INoodleExtensionsObstacle
    {
    }

    public interface ICustomDataEvent : ICustomData, IChromaEvent, INoodleExtensionsEvent
    {
    }
}
