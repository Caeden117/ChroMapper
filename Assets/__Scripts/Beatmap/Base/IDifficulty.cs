using System.Collections.Generic;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class IDifficulty : IItem, ICustomDataDifficulty
    {
        public JSONNode MainNode { get; set; }
        public string DirectoryAndFile { get; set; }
        public abstract string Version { get; }
        public List<IBpmEvent> BpmEvents { get; set; } = new List<IBpmEvent>();
        public List<IRotationEvent> RotationEvents { get; set; } = new List<IRotationEvent>();
        public List<INote> Notes { get; set; } = new List<INote>();
        public List<IBombNote> Bombs { get; set; } = new List<IBombNote>();
        public List<IObstacle> Obstacles { get; set; } = new List<IObstacle>();
        public List<IArc> Arcs { get; set; } = new List<IArc>();
        public List<IChain> Chains { get; set; } = new List<IChain>();
        public List<IWaypoint> Waypoints { get; set; } = new List<IWaypoint>();
        public List<IEvent> Events { get; set; } = new List<IEvent>();
        public List<IColorBoostEvent> ColorBoostEvents { get; set; } = new List<IColorBoostEvent>();

        public virtual List<ILightColorEventBoxGroup<ILightColorEventBox>> LightColorEventBoxGroups { get; set; } =
            new List<ILightColorEventBoxGroup<ILightColorEventBox>>();

        public virtual List<ILightRotationEventBoxGroup<ILightRotationEventBox>> LightRotationEventBoxGroups { get; set; } =
            new List<ILightRotationEventBoxGroup<ILightRotationEventBox>>();

        public IEventTypesWithKeywords EventTypesWithKeywords { get; set; }
        public bool UseNormalEventsAsCompatibleEvents { get; set; } = true;
        public float Time { get; set; } = 0f;
        public List<IBpmChange> BpmChanges { get; set; } = new List<IBpmChange>();
        public List<IBookmark> Bookmarks { get; set; } = new List<IBookmark>();
        public List<ICustomEvent> CustomEvents { get; set; } = new List<ICustomEvent>();

        public List<IEnvironmentEnhancement> EnvironmentEnhancements { get; set; } =
            new List<IEnvironmentEnhancement>();

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
