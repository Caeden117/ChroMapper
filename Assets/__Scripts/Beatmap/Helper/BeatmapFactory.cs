using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V2.Customs;
using Beatmap.V3;
using Beatmap.V3.Customs;
using Beatmap.V4;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Helper
{
    public static class BeatmapFactory
    {
        public static BaseDifficulty GetDifficultyFromJson(JSONNode mainNode, string directoryAndFile)
        {
            var v = PeekMapVersionFromJson(mainNode);

            switch (v[0])
            {
                case '4':
                    Settings.Instance.MapVersion = 4;
                    var difficulty = V4Difficulty.GetFromJson(mainNode, directoryAndFile);
                    var info = BeatSaberSongContainer.Instance.Info;
                    var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
                    V4Difficulty.LoadBpmFromAudioData(difficulty, info);
                    V4Difficulty.LoadLightsFromLightshowFile(difficulty, info, infoDifficulty);
                    return difficulty;
                case '3':
                    Settings.Instance.MapVersion = 3;
                    return V3Difficulty.GetFromJson(mainNode, directoryAndFile);
                case '2':
                    Settings.Instance.MapVersion = 2;
                    return V2Difficulty.GetFromJson(mainNode, directoryAndFile);
                default:
                    return null;
            }
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

        public static BaseNote Note(JSONNode node) => Settings.Instance.MapVersion == 3
            ? node.HasKey("c")
                ? V3ColorNote.GetFromJson(node)
                : V3BombNote.GetFromJson(node)
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
            ? V3Waypoint.GetFromJson(node)
            : V2Waypoint.GetFromJson(node);

        public static BaseEvent Event(JSONNode node)
        {
            if (Settings.Instance.MapVersion == 3)
            {
                if (node.HasKey("e") || node.HasKey("r"))
                    return V3RotationEvent.GetFromJson(node);
                if (node.HasKey("o"))
                    return V3ColorBoostEvent.GetFromJson(node);

                return V3BasicEvent.GetFromJson(node);
            }
            else
            {
                return V2Event.GetFromJson(node);
            }
        }
        
        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> LightColorEventBoxGroups(JSONNode node) =>
            V3LightColorEventBoxGroup.GetFromJson(node);

        public static BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>
            LightRotationEventBoxGroups(JSONNode node) => V3LightRotationEventBoxGroup.GetFromJson(node);

        public static BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>
            LightTranslationEventBoxGroups(JSONNode node) => V3LightTranslationEventBoxGroup.GetFromJson(node);

        public static BaseEventTypesWithKeywords EventTypesWithKeywords(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3BasicEventTypesWithKeywords.GetFromJson(node)
            : V2SpecialEventsKeywordFilters.GetFromJson(node);

        public static BaseBpmChange BpmChange(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3BpmChange.GetFromJson(node)
            : V2BpmChange.GetFromJson(node);

        public static BaseBookmark Bookmark(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3Bookmark.GetFromJson(node)
            : V2Bookmark.GetFromJson(node);

        public static BaseCustomEvent CustomEvent(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3CustomEvent.GetFromJson(node)
            : V2CustomEvent.GetFromJson(node);

        public static BaseEnvironmentEnhancement EnvironmentEnhancement(JSONNode node) => Settings.Instance.MapVersion == 3
            ? V3EnvironmentEnhancement.GetFromJson(node)
            : V2EnvironmentEnhancement.GetFromJson(node);

        // instantiate from good ol parameter

        // public static BaseEventTypesWithKeywords EventTypesWithKeywords(BaseEventTypesForKeywords[] keywords) => Settings.Instance.MapVersion == 3 ? (BaseEventTypesWithKeywords)new V3BasicEventTypesWithKeywords(keywords) : new V2SpecialEventsKeywordFilters(keywords);

        // instantiate from empty

        


        // public static Materials = new Dictionary<string, JSONObject>();
        // public static PointDefinitions = new Dictionary<string, List<JSONArray>>();
    }
}
