using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V2.Customs;
using Beatmap.V3;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Helper
{
    public static class BeatmapFactory
    {
        public static BaseDifficulty GetDifficultyFromJson(JSONNode mainNode, string directoryAndFile)
        {
            var v = PeekMapVersionFromJson(mainNode);
            return v[0] == '3'
                ? V3Difficulty.GetFromJson(mainNode, directoryAndFile)
                : V2Difficulty.GetFromJson(mainNode, directoryAndFile);
        }

        private static string PeekMapVersionFromJson(JSONNode mainNode)
        {
            var nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;
                if (key == "version" || key == "_version") return node.Value;
            }

            Debug.LogError("no version detected, return default version 2.0.0.");
            return "2.0.0";
        }

        public static TConcrete Clone<TConcrete>(TConcrete cloneable) where TConcrete : BaseItem
        {
            if (cloneable is null) throw new ArgumentException("cloneable is null.");
            return cloneable.Clone() as TConcrete;
        }

        // instantiate from JSON
        public static BaseBpmEvent BpmEvent(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3BpmEvent.GetFromJson(node)
            : V2BpmEvent.GetFromJson(node);
        public static BaseRotationEvent RotationEvent(JSONNode node) => new V3RotationEvent(node);

        public static BaseNote Note(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3ColorNote.GetFromJson(node)
            : V2Note.GetFromJson(node);

        public static BaseNote Bomb(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3BombNote.GetFromJson(node)
            : V2Note.GetFromJson(node);

        public static BaseObstacle Obstacle(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3Obstacle.GetFromJson(node)
            : V2Obstacle.GetFromJson(node);

        public static BaseArc Arc(JSONNode node) => Settings.Instance.MapVersion == 3 
            ? V3Arc.GetFromJson(node) 
            : V2Arc.GetFromJson(node);

        public static BaseChain Chain(JSONNode node) => V3Chain.GetFromJson(node);

        public static BaseWaypoint Waypoint(JSONNode node) => Settings.Instance.MapVersion == 3
            ? new V3Waypoint(node)
            : new V2Waypoint(node);

        public static BaseEvent Event(JSONNode node) =>
            Settings.Instance.MapVersion == 3 ? new V3BasicEvent(node) : new V2Event(node);

        public static BaseColorBoostEvent ColorBoostEvent(JSONNode node) => new V3ColorBoostEvent(node);

        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> LightColorEventBoxGroups(JSONNode node) =>
            new V3LightColorEventBoxGroup(node);

        public static BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>
            LightRotationEventBoxGroups(JSONNode node) => new V3LightRotationEventBoxGroup(node);

        public static BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>
            LightTranslationEventBoxGroups(JSONNode node) => new V3LightTranslationEventBoxGroup(node);

        public static BaseEventTypesWithKeywords EventTypesWithKeywords(JSONNode node) => Settings.Instance.MapVersion == 3
            ? new V3BasicEventTypesWithKeywords(node)
            : new V2SpecialEventsKeywordFilters(node);

        public static BaseBpmChange BpmChange(JSONNode node) => Settings.Instance.MapVersion == 3
            ? new V3BpmChange(node)
            : new V2BpmChange(node);

        public static BaseBookmark Bookmark(JSONNode node) => Settings.Instance.MapVersion == 3
            ? new V3Bookmark(node)
            : new V2Bookmark(node);

        public static BaseCustomEvent CustomEvent(JSONNode node) => Settings.Instance.MapVersion == 3
            ? new V3CustomEvent(node)
            : new V2CustomEvent(node);

        public static BaseEnvironmentEnhancement EnvironmentEnhancement(JSONNode node) => Settings.Instance.MapVersion == 3
            ? new V3EnvironmentEnhancement(node)
            : new V2EnvironmentEnhancement(node);

        // instantiate from good ol parameter
        public static BaseRotationEvent RotationEvent(float jsonTime, int executionTime, float rotation,
            JSONNode customData = null) => new V3RotationEvent(jsonTime, executionTime, rotation, customData);

        public static BaseWaypoint Waypoint(float jsonTime, int posX, int posY, int offsetDirection,
            JSONNode customData = null) => Settings.Instance.MapVersion == 3
            ? new V3Waypoint(jsonTime,
                posX, posY, offsetDirection, customData)
            : new V2Waypoint(jsonTime,
                posX, posY, offsetDirection, customData);

        public static BaseEvent
            Event(float jsonTime, int type, int value, float floatValue = 1f, JSONNode customData = null) =>
            Settings.Instance.MapVersion == 3
                ? new V3BasicEvent(jsonTime, type, value, floatValue, customData)
                : new V2Event(jsonTime, type, value, floatValue, customData);

        public static BaseColorBoostEvent ColorBoostEvent(float jsonTime, bool toggle, JSONNode customData = null) =>
            new V3ColorBoostEvent(jsonTime, toggle, customData);

        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> LightColorEventBoxGroups(float jsonTime, int id,
            List<BaseLightColorEventBox> events, JSONNode customData = null) =>
            new V3LightColorEventBoxGroup(jsonTime, id, events, customData);

        public static BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> LightRotationEventBoxGroups(float jsonTime,
            int id, List<BaseLightRotationEventBox> events, JSONNode customData = null) =>
            new V3LightRotationEventBoxGroup(jsonTime, id, events, customData);

        public static BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox> LightTranslationEventBoxGroups(
            float jsonTime,
            int id, List<BaseLightTranslationEventBox> events, JSONNode customData = null) =>
            new V3LightTranslationEventBoxGroup(jsonTime, id, events, customData);

        // public static BaseEventTypesWithKeywords EventTypesWithKeywords(BaseEventTypesForKeywords[] keywords) => Settings.Instance.MapVersion == 3 ? (BaseEventTypesWithKeywords)new V3BasicEventTypesWithKeywords(keywords) : new V2SpecialEventsKeywordFilters(keywords);
        public static BaseBpmChange BpmChange(float jsonTime, float bpm) => Settings.Instance.MapVersion == 3
            ? new V3BpmChange(jsonTime, bpm)
            : new V2BpmChange(jsonTime, bpm);

        public static BaseBookmark Bookmark(float jsonTime, string name) => Settings.Instance.MapVersion == 3
            ? new V3Bookmark(jsonTime, name)
            : new V2Bookmark(jsonTime, name);

        public static BaseCustomEvent CustomEvent(float jsonTime, string type, JSONNode data) => Settings.Instance.MapVersion == 3
            ? new V3CustomEvent(jsonTime, type, data)
            : new V2CustomEvent(jsonTime, type, data);

        // instantiate from empty
        public static BaseRotationEvent RotationEvent() => new V3RotationEvent();

        public static BaseWaypoint Waypoint() =>
            Settings.Instance.MapVersion == 3 ? new V3Waypoint() : new V2Waypoint();

        public static BaseEvent Event() => Settings.Instance.MapVersion == 3 ? new V3BasicEvent() : new V2Event();
        public static BaseColorBoostEvent ColorBoostEvent() => new V3ColorBoostEvent();

        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> LightColorEventBoxGroups() =>
            new V3LightColorEventBoxGroup();

        public static BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> LightRotationEventBoxGroups() =>
            new V3LightRotationEventBoxGroup();

        public static BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>
            LightTranslationEventBoxGroups() => new V3LightTranslationEventBoxGroup();

        public static BaseEventTypesWithKeywords EventTypesWithKeywords() => new V3BasicEventTypesWithKeywords();

        public static BaseBpmChange BpmChange() =>
            Settings.Instance.MapVersion == 3 ? new V3BpmChange() : new V2BpmChange();

        public static BaseBookmark Bookmark() =>
            Settings.Instance.MapVersion == 3 ? new V3Bookmark() : new V2Bookmark();

        public static BaseCustomEvent CustomEvent() => Settings.Instance.MapVersion == 3
            ? new V3CustomEvent()
            : new V2CustomEvent();

        public static BaseEnvironmentEnhancement EnvironmentEnhancement() => Settings.Instance.MapVersion == 3
            ? new V3EnvironmentEnhancement()
            : new V2EnvironmentEnhancement();
        // public static Materials = new Dictionary<string, JSONObject>();
        // public static PointDefinitions = new Dictionary<string, List<JSONArray>>();
    }
}
