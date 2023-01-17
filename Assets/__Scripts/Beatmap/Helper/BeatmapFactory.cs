using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
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
                : V2Difficulty.GetFromJson(mainNode, directoryAndFile) as BaseDifficulty;
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
        public static BaseBpmEvent BpmEvent(JSONNode node) => new V3BpmEvent(node);
        public static BaseRotationEvent RotationEvent(JSONNode node) => new V3RotationEvent(node);

        public static BaseNote Note(JSONNode node) =>
            Settings.Instance.Load_MapV3 ? (BaseNote)new V3ColorNote(node) : new V2Note(node);

        public static BaseNote Bomb(JSONNode node) =>
            Settings.Instance.Load_MapV3 ? (BaseNote)new V3BombNote(node) : new V2Note(node);

        public static BaseObstacle Obstacle(JSONNode node) => Settings.Instance.Load_MapV3
            ? (BaseObstacle)new V3Obstacle(node)
            : new V2Obstacle(node);

        public static BaseArc Arc(JSONNode node) =>
            Settings.Instance.Load_MapV3 ? (BaseArc)new V3Arc(node) : new V2Arc(node);

        public static BaseChain Chain(JSONNode node) => new V3Chain(node);

        public static BaseWaypoint Waypoint(JSONNode node) => Settings.Instance.Load_MapV3
            ? (BaseWaypoint)new V3Waypoint(node)
            : new V2Waypoint(node);

        public static BaseEvent Event(JSONNode node) =>
            Settings.Instance.Load_MapV3 ? (BaseEvent)new V3BasicEvent(node) : new V2Event(node);

        public static BaseColorBoostEvent ColorBoostEvent(JSONNode node) => new V3ColorBoostEvent(node);

        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> LightColorEventBoxGroups(JSONNode node) =>
            new V3LightColorEventBoxGroup(node);

        public static BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>
            LightRotationEventBoxGroups(JSONNode node) => new V3LightRotationEventBoxGroup(node);

        public static BaseEventTypesWithKeywords EventTypesWithKeywords(JSONNode node) => Settings.Instance.Load_MapV3
            ? (BaseEventTypesWithKeywords)new V3BasicEventTypesWithKeywords(node)
            : new V2SpecialEventsKeywordFilters(node);

        public static BaseBpmChange BpmChange(JSONNode node) => Settings.Instance.Load_MapV3
            ? (BaseBpmChange)new V3BpmChange(node)
            : new V2BpmChange(node);

        public static BaseBookmark Bookmark(JSONNode node) => Settings.Instance.Load_MapV3
            ? (BaseBookmark)new V3Bookmark(node)
            : new V2Bookmark(node);

        public static BaseCustomEvent CustomEvent(JSONNode node) => Settings.Instance.Load_MapV3
            ? (BaseCustomEvent)new V3CustomEvent(node)
            : new V2CustomEvent(node);

        public static BaseEnvironmentEnhancement EnvironmentEnhancement(JSONNode node) => Settings.Instance.Load_MapV3
            ? (BaseEnvironmentEnhancement)new V3EnvironmentEnhancement(node)
            : new V2EnvironmentEnhancement(node);

        // instantiate from good ol parameter
        public static BaseBpmEvent BpmEvent(float time, float bpm, JSONNode customData = null) =>
            new V3BpmEvent(time, bpm, customData);

        public static BaseRotationEvent RotationEvent(float time, int executionTime, float rotation,
            JSONNode customData = null) => new V3RotationEvent(time, executionTime, rotation, customData);

        public static BaseNote Note(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
            JSONNode customData = null) => Settings.Instance.Load_MapV3
            ? (BaseNote)new V3ColorNote(time, posX, posY, color, cutDirection, angleOffset, customData)
            : new V2Note(time, posX, posY, color, cutDirection, customData);

        public static BaseNote Bomb(float time, int posX, int posY, JSONNode customData = null) =>
            Settings.Instance.Load_MapV3
                ? (BaseNote)new V3BombNote(time, posX, posY, customData)
                : new V2Note(time, posX, posY, 3, 0, customData);

        public static BaseObstacle Obstacle(float time, int posX, int posY, int type, float duration, int width,
            int height,
            JSONNode customData = null) => Settings.Instance.Load_MapV3
            ? (BaseObstacle)new V3Obstacle(time, posX, posY, duration, width, height, customData)
            : new V2Obstacle(time, posX, type, duration, width, customData);

        public static BaseArc Arc(float time, int color, int posX, int posY, int cutDirection, int angleOffset,
            float mult, float tailTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult,
            int midAnchorMode, JSONNode customData = null) => Settings.Instance.Load_MapV3
            ? (BaseArc)new V3Arc(time, color, posX, posY, cutDirection, angleOffset, mult, tailTime,
                tailPosX, tailPosY, tailCutDirection, tailMult, midAnchorMode, customData)
            : new V2Arc(time, color, posX, posY, cutDirection, angleOffset, mult, tailTime,
                tailPosX, tailPosY, tailCutDirection, tailMult, midAnchorMode, customData);

        public static BaseChain Chain(float time, int color, int posX, int posY, int cutDirection, int angleOffset,
            float tailTime, int tailPosX, int tailPosY, int sliceCount, float squish, JSONNode customData = null) =>
            new V3Chain(time, color, posX, posY, cutDirection, angleOffset, tailTime,
                tailPosX, tailPosY, sliceCount, squish, customData);

        public static BaseWaypoint Waypoint(float time, int posX, int posY, int offsetDirection,
            JSONNode customData = null) => Settings.Instance.Load_MapV3
            ? (BaseWaypoint)new V3Waypoint(time,
                posX, posY, offsetDirection, customData)
            : new V2Waypoint(time,
                posX, posY, offsetDirection, customData);

        public static BaseEvent
            Event(float time, int type, int value, float floatValue = 1f, JSONNode customData = null) =>
            Settings.Instance.Load_MapV3
                ? (BaseEvent)new V3BasicEvent(time, type, value, floatValue, customData)
                : new V2Event(time, type, value, floatValue, customData);

        public static BaseColorBoostEvent ColorBoostEvent(float time, bool toggle, JSONNode customData = null) =>
            new V3ColorBoostEvent(time, toggle, customData);

        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> LightColorEventBoxGroups(float time, int id,
            List<BaseLightColorEventBox> events, JSONNode customData = null) =>
            new V3LightColorEventBoxGroup(time, id, events, customData);

        public static BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> LightRotationEventBoxGroups(float time,
            int id, List<BaseLightRotationEventBox> events, JSONNode customData = null) =>
            new V3LightRotationEventBoxGroup(time, id, events, customData);

        // public static BaseEventTypesWithKeywords EventTypesWithKeywords(BaseEventTypesForKeywords[] keywords) => Settings.Instance.Load_MapV3 ? (BaseEventTypesWithKeywords)new V3BasicEventTypesWithKeywords(keywords) : new V2SpecialEventsKeywordFilters(keywords);
        public static BaseBpmChange BpmChange(float time, float bpm) => Settings.Instance.Load_MapV3
            ? (BaseBpmChange)new V3BpmChange(time, bpm)
            : new V2BpmChange(time, bpm);

        public static BaseBookmark Bookmark(float time, string name) => Settings.Instance.Load_MapV3
            ? (BaseBookmark)new V3Bookmark(time, name)
            : new V2Bookmark(time, name);

        // instantiate from empty
        public static BaseBpmEvent BpmEvent() => new V3BpmEvent();
        public static BaseRotationEvent RotationEvent() => new V3RotationEvent();
        public static BaseNote Note() => Settings.Instance.Load_MapV3 ? (BaseNote)new V3ColorNote() : new V2Note();
        public static BaseNote Bomb() => Settings.Instance.Load_MapV3 ? (BaseNote)new V3BombNote() : new V2Note();

        public static BaseObstacle Obstacle() =>
            Settings.Instance.Load_MapV3 ? (BaseObstacle)new V3Obstacle() : new V2Obstacle();

        public static BaseArc Arc() => Settings.Instance.Load_MapV3 ? (BaseArc)new V3Arc() : new V2Arc();
        public static BaseChain Chain() => new V3Chain();

        public static BaseWaypoint Waypoint() =>
            Settings.Instance.Load_MapV3 ? (BaseWaypoint)new V3Waypoint() : new V2Waypoint();

        public static BaseEvent Event() => Settings.Instance.Load_MapV3 ? (BaseEvent)new V3BasicEvent() : new V2Event();
        public static BaseColorBoostEvent ColorBoostEvent() => new V3ColorBoostEvent();

        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> LightColorEventBoxGroups() =>
            new V3LightColorEventBoxGroup();

        public static BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> LightRotationEventBoxGroups() =>
            new V3LightRotationEventBoxGroup();

        public static BaseEventTypesWithKeywords EventTypesWithKeywords() => Settings.Instance.Load_MapV3
            ? (BaseEventTypesWithKeywords)new V3BasicEventTypesWithKeywords()
            : new V2SpecialEventsKeywordFilters();

        public static BaseBpmChange BpmChange() =>
            Settings.Instance.Load_MapV3 ? (BaseBpmChange)new V3BpmChange() : new V2BpmChange();

        public static BaseBookmark Bookmark() =>
            Settings.Instance.Load_MapV3 ? (BaseBookmark)new V3Bookmark() : new V2Bookmark();

        public static BaseCustomEvent CustomEvent() => Settings.Instance.Load_MapV3
            ? (BaseCustomEvent)new V3CustomEvent()
            : new V2CustomEvent();

        public static BaseEnvironmentEnhancement EnvironmentEnhancement() => Settings.Instance.Load_MapV3
            ? (BaseEnvironmentEnhancement)new V3EnvironmentEnhancement()
            : new V2EnvironmentEnhancement();
        // public static Materials = new Dictionary<string, JSONObject>();
        // public static PointDefinitions = new Dictionary<string, List<JSONArray>>();
    }
}
