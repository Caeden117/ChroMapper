using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V4
{
    public class V4Difficulty
    {
        private const string version = "4.0.0";

        public static JSONNode GetOutputJson(BaseDifficulty difficulty)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                var json = new JSONObject { ["version"] = version };

                // TODO: BpmEvents need to be saved into separate file
                
                // TODO: Non-interactable objects in separate file
                
                // TODO: WIP

                
                // Notes
                var colorNotes = new JSONArray();
                var colorNotesData = new JSONArray();

                var notes = difficulty.Notes.Where(x => x.Type != (int)NoteType.Bomb).ToList();

                var notesCommonData = notes.Select(V4CommonData.Note.FromBaseNote).Distinct().ToList();
                
                foreach (var note in notes)
                {
                    colorNotes.Add(V4ColorNote.ToJson(note, notesCommonData));
                }

                foreach (var noteData in notesCommonData)
                {
                    colorNotesData.Add(noteData.ToJson());
                }

                json["colorNotes"] = colorNotes;
                json["colorNotesData"] = colorNotesData;
                
                
                var bombNotes = new JSONArray();
                var bombNotesData = new JSONArray();
                var bombs = difficulty.Notes.Where(x => x.Type == (int)NoteType.Bomb).ToList();

                if (Settings.Instance.SaveWithoutDefaultValues)
                {
                    SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);
                }

                return json;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError(
                    "This is bad. You are recommended to restart ChroMapper; progress made after this point is not guaranteed to be saved.");
                return null;
            }
        }

        // TODO: WIP
        public static BaseDifficulty GetFromJson(JSONNode mainNode, string path)
        {
            try
            {
                var map = new BaseDifficulty { DirectoryAndFile = path, Version = version };

                // Get common Data
                var notesCommonData = new List<V4CommonData.Note>();
                var bombsCommonData = new List<V4CommonData.Bomb>();
                var obstaclesCommonData = new List<V4CommonData.Obstacle>();
                var arcsCommonData = new List<V4CommonData.Arc>();
                var chainsCommonData = new List<V4CommonData.Chain>();
                var rotationsCommonData = new List<V4CommonData.RotationEvent>();
                
                var nodeEnum = mainNode.GetEnumerator();
                while (nodeEnum.MoveNext())
                {
                    var key = nodeEnum.Current.Key;
                    var node = nodeEnum.Current.Value;

                    switch (key)
                    {
                        case "colorNotesData":
                            foreach (JSONNode n in node)
                            {
                                notesCommonData.Add(V4CommonData.Note.GetFromJson(n));
                            }
                            break;
                        case "bombNotesData":
                            foreach (JSONNode n in node)
                            {
                                bombsCommonData.Add(V4CommonData.Bomb.GetFromJson(n));
                            }
                            break;
                        case "obstaclesData":
                            foreach (JSONNode n in node)
                            {
                                obstaclesCommonData.Add(V4CommonData.Obstacle.GetFromJson(n));
                            }
                            break;
                        case "arcsData":
                            foreach (JSONNode n in node)
                            {
                                arcsCommonData.Add(V4CommonData.Arc.GetFromJson(n));
                            }
                            break;
                        case "chainsData":
                            foreach (JSONNode n in node)
                            {
                                chainsCommonData.Add(V4CommonData.Chain.GetFromJson(n));
                            }
                            break;
                        case "spawnRotationsData":
                            foreach (JSONNode n in node)
                            {
                                rotationsCommonData.Add(V4CommonData.RotationEvent.GetFromJson(n));
                            }
                            break;
                    }
                }
                
                // Get the actual things we're working with
                nodeEnum = mainNode.GetEnumerator();
                while (nodeEnum.MoveNext())
                {
                    var key = nodeEnum.Current.Key;
                    var node = nodeEnum.Current.Value;

                    switch (key)
                    {
                        case "colorNotes":
                            foreach (JSONNode n in node)
                            {
                                map.Notes.Add(V4ColorNote.GetFromJson(n, notesCommonData));
                            }
                            break;
                        case "bombNotes":
                            foreach (JSONNode n in node)
                            {
                                map.Notes.Add(V4BombNote.GetFromJson(n, bombsCommonData));
                            }

                            break;
                        case "obstacles":
                            foreach (JSONNode n in node)
                            {
                                map.Obstacles.Add(V4Obstacle.GetFromJson(n, obstaclesCommonData));
                            }

                            break;
                        case "arcs":
                            foreach (JSONNode n in node)
                            {
                                map.Arcs.Add(V4Arc.GetFromJson(n, notesCommonData, arcsCommonData));
                            }

                            break;
                        case "chains":
                            foreach (JSONNode n in node)
                            {
                                map.Chains.Add(V4Chain.GetFromJson(n, notesCommonData, chainsCommonData));
                            }

                            break;
                        case "spawnRotations":
                            foreach (JSONNode n in node)
                            {
                                map.Events.Add(V4RotationEvent.GetFromJson(n, rotationsCommonData));
                            }

                            break;
                    }
                }

                // Important!
                map.Notes.Sort();
                map.Events.Sort();
                map.Obstacles.Sort();
                map.Chains.Sort();
                map.Arcs.Sort();

                return map;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public static void LoadBpmFromAudioData(BaseDifficulty map, BaseInfo info)
        {
            var bpmInfo = V4AudioData.GetFromJson(BeatSaberSongUtils.GetNodeFromFile(Path.Combine(info.Directory, info.AudioDataFilename)));

            var bpmEvents = BaseBpmInfo.GetBpmEvents(bpmInfo.BpmRegions, bpmInfo.AudioFrequency);
            map.BpmEvents = bpmEvents;
        }

        public static void LoadLightsFromLightshowFile(BaseDifficulty map, BaseInfo info, InfoDifficulty infoDifficulty)
        {
            var path = Path.Combine(info.Directory, infoDifficulty.LightshowFileName);
            var mainNode = BeatSaberSongUtils.GetNodeFromFile(path);

            // Get common Data
            var basicEventsCommonData = new List<V4CommonData.BasicEvent>();
            var colorBoostEventsCommonData = new List<V4CommonData.ColorBoostEvent>();
            var waypointsCommonData = new List<V4CommonData.Waypoint>();

            var indexFilters = new List<BaseIndexFilter>();

            var lightColorEventBoxesCommonData = new List<V4CommonData.LightColorEventBox>();
            var lightColorEventsCommonData = new List<V4CommonData.LightColorEvent>();
            
            var lightRotationEventBoxesCommonData = new List<V4CommonData.LightRotationEventBox>();
            var lightRotationEventsCommonData = new List<V4CommonData.LightRotationEvent>();
            
            var lightTranslationEventBoxesCommonData = new List<V4CommonData.LightTranslationEventBox>();
            var lightTranslationEventsCommonData = new List<V4CommonData.LightTranslationEvent>();

            var fxEventBoxesCommonData = new List<V4CommonData.FxEventBox>();
            var floatFxEventsCommonData = new List<V4CommonData.FloatFxEvent>();
            
            var nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;

                switch (key)
                {
                    case "basicEventsData":
                        foreach (JSONNode n in node)
                        {
                            basicEventsCommonData.Add(V4CommonData.BasicEvent.GetFromJson(n));
                        }

                        break;
                    case "RotationBoostEventsData":
                        foreach (JSONNode n in node)
                        {
                            colorBoostEventsCommonData.Add(V4CommonData.ColorBoostEvent.GetFromJson(n));
                        }

                        break;
                    case "waypointsData":
                        foreach (JSONNode n in node)
                        {
                            waypointsCommonData.Add(V4CommonData.Waypoint.GetFromJson(n));
                        }

                        break;
                    
                    case "indexFilters":
                        foreach (JSONNode n in node)
                        {
                            indexFilters.Add(V4IndexFilter.GetFromJson(n));
                        }

                        break;
                    
                    case "lightColorEventBoxes":
                        foreach (JSONNode n in node)
                        {
                            lightColorEventsCommonData.Add(V4CommonData.LightColorEvent.GetFromJson(n));
                        }

                        break;
                    case "lightColorEvents":
                        foreach (JSONNode n in node)
                        {
                            lightColorEventBoxesCommonData.Add(V4CommonData.LightColorEventBox.GetFromJson(n));
                        }

                        break;
                    case "lightRotationEventBoxes":
                        foreach (JSONNode n in node)
                        {
                            lightRotationEventsCommonData.Add(V4CommonData.LightRotationEvent.GetFromJson(n));
                        }

                        break;
                    case "lightRotationEvents":
                        foreach (JSONNode n in node)
                        {
                            lightRotationEventBoxesCommonData.Add(V4CommonData.LightRotationEventBox.GetFromJson(n));
                        }

                        break;
                    case "lightTranslationEventBoxes":
                        foreach (JSONNode n in node)
                        {
                            lightTranslationEventsCommonData.Add(V4CommonData.LightTranslationEvent.GetFromJson(n));
                        }

                        break;
                    case "lightTranslationEvents":
                        foreach (JSONNode n in node)
                        {
                            lightTranslationEventBoxesCommonData.Add(V4CommonData.LightTranslationEventBox.GetFromJson(n));
                        }
                        break;
                    
                    case "fxEventBoxes":
                        foreach (JSONNode n in node)
                        {
                            fxEventBoxesCommonData.Add(V4CommonData.FxEventBox.GetFromJson(n));
                        }

                        break;
                    
                    case "floatFxEvents":
                        foreach (JSONNode n in node)
                        {
                            floatFxEventsCommonData.Add(V4CommonData.FloatFxEvent.GetFromJson(n));
                        }

                        break;
                }
            }

            // Get the actual things we're working with
            nodeEnum = mainNode.GetEnumerator();
            while (nodeEnum.MoveNext())
            {
                var key = nodeEnum.Current.Key;
                var node = nodeEnum.Current.Value;

                switch (key)
                {
                    case "basicEvents":
                        foreach (JSONNode n in node)
                        {
                            map.Events.Add(V4BasicEvent.GetFromJson(n, basicEventsCommonData));
                        }

                        break;
                    
                    case "colorBoostEvents":
                        foreach (JSONNode n in node)
                        {
                            map.Events.Add(V4ColorBoostEvent.GetFromJson(n, colorBoostEventsCommonData));
                        }

                        break;
                    
                    // Pain
                    case "eventBoxGroups":
                        foreach (JSONNode n in node)
                        {
                            var type = node["t"].AsInt;
                            switch (type)
                            {
                                case 1: // Light Color
                                    map.LightColorEventBoxGroups.Add(V4LightColorEventBoxGroup.GetFromJson(n,
                                        indexFilters, lightColorEventBoxesCommonData, lightColorEventsCommonData));
                                    break;
                                case 2: // Light Rotation
                                    map.LightRotationEventBoxGroups.Add(V4LightRotationEventBoxGroup.GetFromJson(n,
                                        indexFilters, lightRotationEventBoxesCommonData,
                                        lightRotationEventsCommonData));
                                    break;
                                case 3: // Light Translation
                                    map.LightTranslationEventBoxGroups.Add(V4LightTranslationEventBoxGroup.GetFromJson(
                                        n, indexFilters, lightTranslationEventBoxesCommonData,
                                        lightTranslationEventsCommonData));
                                    break;
                                case 4: // FX Events
                                    map.VfxEventBoxGroups.Add(V4VfxEventEventBoxGroup.GetFromJson(n, indexFilters,
                                        fxEventBoxesCommonData, floatFxEventsCommonData));
                                    break;
                            }
                        }
                    
                        break;
                    
                    case "waypoints":
                        foreach (JSONNode n in node)
                        {
                            map.Waypoints.Add(V4Waypoint.GetFromJson(n, waypointsCommonData)); 
                        }
                        break;
                    
                    
                    case "basicEventTypesWithKeywords":
                        map.EventTypesWithKeywords = V4BasicEventTypesWithKeywords.GetFromJson(node);
                        break;
                    
                    case "useNormalEventsAsCompatibleEvents":
                        map.UseNormalEventsAsCompatibleEvents = node.AsBool;
                        break;
                }
            }

            // Important!
            map.Events.Sort();
        }
    }
}
