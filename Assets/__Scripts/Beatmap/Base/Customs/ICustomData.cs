using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public interface ICustomData
    {
        JSONNode CustomData { get; set; }
        JSONNode GetOrCreateCustom();
        void ParseCustom();

        bool IsChroma();
        bool IsNoodleExtensions();
        bool IsMappingExtensions();
    }

    public interface ICustomDataDifficulty : ICustomData
    {
        float Time { get; set; }
        List<IBpmChange> BpmChanges { get; set; }
        List<IBookmark> Bookmarks { get; set; }
        List<ICustomEvent> CustomEvents { get; set; }
        List<IEnvironmentEnhancement> EnvironmentEnhancements { get; set; }
    }

    public interface ICustomDataNote : ICustomData, IChromaObject, INoodleExtensionsNote
    {
    }

    public interface ICustomDataBomb : ICustomData, IChromaObject, INoodleExtensionsGrid
    {
    }

    public interface ICustomDataArc : ICustomData, IChromaObject, INoodleExtensionsGrid
    {
    }

    public interface ICustomDataChain : ICustomData, IChromaObject, INoodleExtensionsGrid
    {
    }

    public interface ICustomDataObstacle : ICustomData, IChromaObject, INoodleExtensionsObstacle
    {
    }

    public interface ICustomDataEvent : ICustomData, IChromaEvent, INoodleExtensionsEvent
    {
    }
}
