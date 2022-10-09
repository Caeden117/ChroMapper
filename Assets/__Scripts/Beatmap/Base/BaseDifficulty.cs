using System.Collections.Generic;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseDifficulty : BaseItem, ICustomDataDifficulty
    {
        public JSONNode MainNode { get; set; }
        public string DirectoryAndFile { get; set; }
        public abstract string Version { get; }
        public List<BaseBpmEvent> BpmEvents { get; set; } = new List<BaseBpmEvent>();
        public List<BaseRotationEvent> RotationEvents { get; set; } = new List<BaseRotationEvent>();
        public List<BaseNote> Notes { get; set; } = new List<BaseNote>();
        public List<BaseBombNote> Bombs { get; set; } = new List<BaseBombNote>();
        public List<BaseObstacle> Obstacles { get; set; } = new List<BaseObstacle>();
        public List<BaseArc> Arcs { get; set; } = new List<BaseArc>();
        public List<BaseChain> Chains { get; set; } = new List<BaseChain>();
        public List<BaseWaypoint> Waypoints { get; set; } = new List<BaseWaypoint>();
        public List<BaseEvent> Events { get; set; } = new List<BaseEvent>();
        public List<BaseColorBoostEvent> ColorBoostEvents { get; set; } = new List<BaseColorBoostEvent>();

        public virtual List<BaseLightColorEventBoxGroup<BaseLightColorEventBox>> LightColorEventBoxGroups { get; set; } =
            new List<BaseLightColorEventBoxGroup<BaseLightColorEventBox>>();

        public virtual List<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>> LightRotationEventBoxGroups { get; set; } =
            new List<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>();

        public BaseEventTypesWithKeywords EventTypesWithKeywords { get; set; }
        public bool UseNormalEventsAsCompatibleEvents { get; set; } = true;
        public float Time { get; set; } = 0f;
        public List<BaseBpmEvent> BpmChanges { get; set; } = new List<BaseBpmEvent>();
        public List<BaseBookmark> Bookmarks { get; set; } = new List<BaseBookmark>();
        public List<BaseCustomEvent> CustomEvents { get; set; } = new List<BaseCustomEvent>();

        public List<BaseEnvironmentEnhancement> EnvironmentEnhancements { get; set; } =
            new List<BaseEnvironmentEnhancement>();

        public JSONNode CustomData { get; set; }

        public JSONNode GetOrCreateCustom()
        {
            if (CustomData == null)
                CustomData = new JSONObject();

            return CustomData;
        }

        public void ParseCustom() => throw new System.NotSupportedException();

        public abstract bool IsChroma();
        public abstract bool IsNoodleExtensions();
        public abstract bool IsMappingExtensions();

        public abstract bool Save();
        public abstract int GetVersion();

        protected abstract bool SaveCustomDataNode();

        // Cleans an array by filtering out null elements, or objects with invalid time.
        // Could definitely be optimized a little bit, but since saving is done on a separate thread, I'm not too worried about it.
        protected static JSONArray CleanupArray(JSONArray original, string timeKey = "_time")
        {
            var array = original.Clone().AsArray;
            foreach (JSONNode node in original)
                if (node is null || node[timeKey].IsNull || float.IsNaN(node[timeKey]))
                    array.Remove(node);

            return array;
        }

        // fuick
        // public static abstract IDifficulty GetFromJson(JSONNode node, string path);
    }
}
