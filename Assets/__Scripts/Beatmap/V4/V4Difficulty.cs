using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V4
{
    public class V4Difficulty
    {
        public const string BeatmapVersion = "4.1.0";
        private const string lightshowVersion = "4.0.0";

        public static JSONNode GetOutputJson(BaseDifficulty difficulty)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                var json = new JSONObject { ["version"] = BeatmapVersion };
                
                // LINQ abuse

                // Color notes
                var colorNotes = new JSONArray();
                var colorNotesData = new JSONArray();
                
                var notes = difficulty.Notes.Where(x => x.Type != (int)NoteType.Bomb).ToList();
                List<V4CommonData.Note> colorNotesCommonData = new(); 
                colorNotesCommonData.AddRange(notes.Select(V4CommonData.Note.FromBaseNote));
                colorNotesCommonData.AddRange(difficulty.Arcs.Select(V4CommonData.Note.FromBaseSliderHead));
                colorNotesCommonData.AddRange(difficulty.Arcs.Select(V4CommonData.Note.FromBaseArcTail));
                colorNotesCommonData.AddRange(difficulty.Chains.Select(V4CommonData.Note.FromBaseSliderHead));
                colorNotesCommonData = colorNotesCommonData.Distinct().ToList();
                
                foreach (var note in notes)
                {
                    colorNotes.Add(V4ColorNote.ToJson(note, colorNotesCommonData));
                }

                foreach (var noteData in colorNotesCommonData)
                {
                    colorNotesData.Add(noteData.ToJson());
                }

                json["colorNotes"] = colorNotes;
                json["colorNotesData"] = colorNotesData;
                
                // Bombs
                var bombNotes = new JSONArray();
                var bombNotesData = new JSONArray();
                var bombs = difficulty.Notes.Where(x => x.Type == (int)NoteType.Bomb).ToList();
                var bombNotesCommonData = bombs.Select(V4CommonData.Bomb.FromBaseNote).Distinct().ToList();
                
                foreach (var bomb in bombs)
                {
                    bombNotes.Add(V4BombNote.ToJson(bomb, bombNotesCommonData));
                }

                foreach (var bombData in bombNotesCommonData)
                {
                    bombNotesData.Add(bombData.ToJson());
                }

                json["bombNotes"] = bombNotes;
                json["bombNotesData"] = bombNotesData;
                
                // Arcs
                var arcs = new JSONArray();
                var arcsData = new JSONArray();
                var arcsCommonData = difficulty.Arcs.Select(V4CommonData.Arc.FromBaseArc).Distinct().ToList();
                
                foreach (var arc in difficulty.Arcs)
                {
                    arcs.Add(V4Arc.ToJson(arc, colorNotesCommonData, arcsCommonData));   
                }

                foreach (var arcData in arcsCommonData)
                {
                    arcsData.Add(arcData.ToJson());
                }

                json["arcs"] = arcs;
                json["arcsData"] = arcsData;
                
                // Chains
                var chains = new JSONArray();
                var chainsData = new JSONArray();
                var chainsCommonData = difficulty.Chains.Select(V4CommonData.Chain.FromBaseChain).Distinct().ToList();
                
                foreach (var chain in difficulty.Chains)
                {
                    chains.Add(V4Chain.ToJson(chain, colorNotesCommonData, chainsCommonData));   
                }

                foreach (var chainData in chainsCommonData)
                {
                    chainsData.Add(chainData.ToJson());
                }

                json["chains"] = chains;
                json["chainsData"] = chainsData;
                
                // Obstacles
                var obstacles = new JSONArray();
                var obstaclesData = new JSONArray();
                var obstaclesCommonData = difficulty.Obstacles.Select(V4CommonData.Obstacle.FromBaseObstacle).Distinct().ToList();
                
                foreach (var obstacle in difficulty.Obstacles)
                {
                    obstacles.Add(V4Obstacle.ToJson(obstacle, obstaclesCommonData));   
                }

                foreach (var obstacleData in obstaclesCommonData)
                {
                    obstaclesData.Add(obstacleData.ToJson());
                }

                json["obstacles"] = obstacles;
                json["obstaclesData"] = obstaclesData;
                
                // NJS events
                var njsEvents = new JSONArray();
                var njsEventsData = new JSONArray();
                var njsEventsCommonData = difficulty.NJSEvents.Select(V4CommonData.NJSEvent.FromBaseNJSEvent).Distinct().ToList();

                foreach (var njsEvent in difficulty.NJSEvents)
                {
                    njsEvents.Add(V4NJSEvent.ToJson(njsEvent, njsEventsCommonData));
                }

                foreach (var njsEventData in njsEventsCommonData)
                {
                    njsEventsData.Add(njsEventData.ToJson());
                }
                
                json["njsEvents"] = njsEvents;
                json["njsEventData"] = njsEventsData; // Yup there's no 's' on njs event
                
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
        
        public static JSONNode GetLightshowOutputJson(BaseDifficulty difficulty)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                var json = new JSONObject { ["version"] = lightshowVersion };

                // Basic events
                var basicEvents = new JSONArray();
                var basicEventsData = new JSONArray();
                var mapBasicEvents = difficulty.Events.Where(x => !x.IsLaneRotationEvent() && !x.IsColorBoostEvent()).ToList();
                var basicEventsCommonData = mapBasicEvents.Select(V4CommonData.BasicEvent.FromBaseEvent).Distinct().ToList();
                
                foreach (var basicEvent in mapBasicEvents)
                {
                    basicEvents.Add(V4BasicEvent.ToJson(basicEvent, basicEventsCommonData));
                }

                foreach (var basicEventData in basicEventsCommonData)
                {
                    basicEventsData.Add(basicEventData.ToJson());
                }

                json["basicEvents"] = basicEvents;
                json["basicEventsData"] = basicEventsData;
                
                // Boost events
                var colorBoostEvents = new JSONArray();
                var colorBoostEventsData = new JSONArray();
                var mapColorBoostEvents = difficulty.Events.Where(x => x.IsColorBoostEvent()).ToList();
                var colorBoostEventsCommonData = mapColorBoostEvents.Select(V4CommonData.ColorBoostEvent.FromBaseEvent).Distinct().ToList();
                
                foreach (var colorBoostEvent in mapColorBoostEvents)
                {
                    colorBoostEvents.Add(V4ColorBoostEvent.ToJson(colorBoostEvent, colorBoostEventsCommonData));
                }

                foreach (var colorBoostEventData in colorBoostEventsCommonData)
                {
                    colorBoostEventsData.Add(colorBoostEventData.ToJson());
                }

                json["colorBoostEvents"] = colorBoostEvents;
                json["colorBoostEventsData"] = colorBoostEventsData;
                
                // Waypoints
                var waypoints = new JSONArray();
                var waypointsData = new JSONArray();
                var waypointsCommonData = difficulty.Waypoints.Select(V4CommonData.Waypoint.FromBaseWayPoint).Distinct().ToList();
                
                foreach (var waypoint in difficulty.Waypoints)
                {
                    waypoints.Add(V4Waypoint.ToJson(waypoint, waypointsCommonData));
                }

                foreach (var waypointData in waypointsCommonData)
                {
                    waypointsData.Add(waypointData.ToJson());
                }

                json["waypoints"] = waypoints;
                json["waypointsData"] = waypointsData;
                
                // Group lighting
                
                
                // Event groups
                var eventBoxGroups = new JSONArray();
                
                // Get index filters from group events
                var indexFiltersCommonData = new List<V4CommonData.IndexFilter>();
                indexFiltersCommonData.AddRange(difficulty.LightColorEventBoxGroups.SelectMany(box => box.Events)
                    .Select(evt => evt.IndexFilter).Select(V4CommonData.IndexFilter.FromBaseIndexFilter));
                indexFiltersCommonData.AddRange(difficulty.LightRotationEventBoxGroups.SelectMany(box => box.Events)
                    .Select(evt => evt.IndexFilter).Select(V4CommonData.IndexFilter.FromBaseIndexFilter));
                indexFiltersCommonData.AddRange(difficulty.LightTranslationEventBoxGroups.SelectMany(box => box.Events)
                    .Select(evt => evt.IndexFilter).Select(V4CommonData.IndexFilter.FromBaseIndexFilter));
                indexFiltersCommonData.AddRange(difficulty.VfxEventBoxGroups.SelectMany(box => box.Events)
                    .Select(evt => evt.IndexFilter).Select(V4CommonData.IndexFilter.FromBaseIndexFilter));
                indexFiltersCommonData = indexFiltersCommonData.Distinct().ToList();

                var indexFilters = new JSONArray();
                foreach (var indexFilterData in indexFiltersCommonData)
                {
                    indexFilters.Add(indexFilterData.ToJson());
                }

                json["indexFilters"] = indexFilters;
                
                // Color Group events
                var lightColorEventBoxes = new JSONArray();
                var lightColorEvents = new JSONArray();

                var lightColorEventBoxesCommonData = difficulty.LightColorEventBoxGroups
                    .SelectMany(group => group.Events)
                    .Select(V4CommonData.LightColorEventBox.FromBaseLightColorEventBox)
                    .ToList();
                
                var lightColorEventsCommonData = difficulty.LightColorEventBoxGroups
                    .SelectMany(group => group.Events)
                    .SelectMany(box => box.Events)
                    .Select(V4CommonData.LightColorEvent.FromBaseLightColorEvent)
                    .ToList();

                foreach (var lightColorBoxData in lightColorEventBoxesCommonData)
                {
                    lightColorEventBoxes.Add(lightColorBoxData.ToJson());
                }

                foreach (var lightColorEventData in lightColorEventsCommonData)
                {
                    lightColorEvents.Add(lightColorEventData.ToJson());
                }

                foreach (var groupEvent in difficulty.LightColorEventBoxGroups)
                {
                    eventBoxGroups.Add(V4LightColorEventBoxGroup.ToJson(groupEvent, indexFiltersCommonData,
                        lightColorEventBoxesCommonData, lightColorEventsCommonData));
                }
                
                // Rotation Group events
                var lightRotationEventBoxes = new JSONArray();
                var lightRotationEvents = new JSONArray();

                var lightRotationEventBoxesCommonData = difficulty.LightRotationEventBoxGroups
                    .SelectMany(group => group.Events)
                    .Select(V4CommonData.LightRotationEventBox.FromBaseLightRotationEventBox)
                    .ToList();
                
                var lightRotationEventsCommonData = difficulty.LightRotationEventBoxGroups
                    .SelectMany(group => group.Events)
                    .SelectMany(box => box.Events)
                    .Select(V4CommonData.LightRotationEvent.FromBaseLightRotationEvent)
                    .ToList();

                foreach (var lightRotationBoxData in lightRotationEventBoxesCommonData)
                {
                    lightRotationEventBoxes.Add(lightRotationBoxData.ToJson());
                }

                foreach (var lightRotationEventData in lightRotationEventsCommonData)
                {
                    lightRotationEvents.Add(lightRotationEventData.ToJson());
                }

                foreach (var groupEvent in difficulty.LightRotationEventBoxGroups)
                {
                    eventBoxGroups.Add(V4LightRotationEventBoxGroup.ToJson(groupEvent, indexFiltersCommonData,
                        lightRotationEventBoxesCommonData, lightRotationEventsCommonData));
                }
                
                // Translation Group events
                var lightTranslationEventBoxes = new JSONArray();
                var lightTranslationEvents = new JSONArray();

                var lightTranslationEventBoxesCommonData = difficulty.LightTranslationEventBoxGroups
                    .SelectMany(group => group.Events)
                    .Select(V4CommonData.LightTranslationEventBox.FromBaseLightTranslationEventBox)
                    .ToList();
                
                var lightTranslationEventsCommonData = difficulty.LightTranslationEventBoxGroups
                    .SelectMany(group => group.Events)
                    .SelectMany(box => box.Events)
                    .Select(V4CommonData.LightTranslationEvent.FromBaseLightTranslationEvent)
                    .ToList();

                foreach (var lightTranslationBoxData in lightTranslationEventBoxesCommonData)
                {
                    lightTranslationEventBoxes.Add(lightTranslationBoxData.ToJson());
                }

                foreach (var lightTranslationEventData in lightTranslationEventsCommonData)
                {
                    lightTranslationEvents.Add(lightTranslationEventData.ToJson());
                }

                foreach (var groupEvent in difficulty.LightTranslationEventBoxGroups)
                {
                    eventBoxGroups.Add(V4LightTranslationEventBoxGroup.ToJson(groupEvent, indexFiltersCommonData,
                        lightTranslationEventBoxesCommonData, lightTranslationEventsCommonData));
                }
                
                // Float FX effects
                // Translation Group events
                var fxEventBoxes = new JSONArray();
                var floatFxEvents = new JSONArray();

                var fxEventBoxesCommonData = difficulty.VfxEventBoxGroups
                    .SelectMany(group => group.Events)
                    .Select(V4CommonData.FxEventBox.FromBaseFxEventBox)
                    .ToList();
                
                var floatFxEventsCommonData = difficulty.VfxEventBoxGroups
                    .SelectMany(group => group.Events)
                    .SelectMany(box => box.FloatFxEvents)
                    .Select(V4CommonData.FloatFxEvent.FromFloatFxEventBase)
                    .ToList();

                foreach (var fxEventBoxData in fxEventBoxesCommonData)
                {
                    fxEventBoxes.Add(fxEventBoxData.ToJson());
                }

                foreach (var floatFxEvent in floatFxEventsCommonData)
                {
                    floatFxEvents.Add(floatFxEvent.ToJson());
                }

                foreach (var groupEvent in difficulty.VfxEventBoxGroups)
                {
                    eventBoxGroups.Add(V4VfxEventEventBoxGroup.ToJson(groupEvent, indexFiltersCommonData,
                        fxEventBoxesCommonData, floatFxEventsCommonData));
                }
                
                // Order the event box groups by time, group, and type
                var orderedEventBoxGroups = eventBoxGroups.Linq
                    .OrderBy(x => x.Value["b"].AsInt)
                    .ThenBy(x => x.Value["g"].AsInt)
                    .ThenBy(x => x.Value["t"].AsInt)
                    .Select(x => x.Value);

                eventBoxGroups = new JSONArray();
                foreach (var eventBoxGroup in orderedEventBoxGroups)
                {
                    eventBoxGroups.Add(eventBoxGroup);
                }

                json["eventBoxGroups"] = eventBoxGroups;

                json["lightColorEventBoxes"] = lightColorEventBoxes;
                json["lightColorEvents"] = lightColorEvents;
                
                json["lightRotationEventBoxes"] = lightRotationEventBoxes;
                json["lightRotationEvents"] = lightRotationEvents;
                
                json["lightTranslationEventBoxes"] = lightTranslationEventBoxes;
                json["lightTranslationEvents"] = lightTranslationEvents;
                
                json["fxEventBoxes"] = fxEventBoxes;
                json["floatFxEvents"] = floatFxEvents;
                
                // Simple properties
                json["basicEventTypesWithKeywords"] = difficulty.EventTypesWithKeywords?.ToJson() ?? new BaseEventTypesWithKeywords().ToJson();
                json["useNormalEventsAsCompatibleEvents"] = difficulty.UseNormalEventsAsCompatibleEvents;
                
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

        public static BaseDifficulty GetFromJson(JSONNode mainNode, string path)
        {
            try
            {
                var map = new BaseDifficulty { DirectoryAndFile = path, Version = BeatmapVersion };

                // Get common Data
                var notesCommonData = new List<V4CommonData.Note>();
                var bombsCommonData = new List<V4CommonData.Bomb>();
                var obstaclesCommonData = new List<V4CommonData.Obstacle>();
                var arcsCommonData = new List<V4CommonData.Arc>();
                var chainsCommonData = new List<V4CommonData.Chain>();
                var rotationsCommonData = new List<V4CommonData.RotationEvent>();
                var njsEventsCommonData = new List<V4CommonData.NJSEvent>();
                
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
                        case "njsEventData": // Yup there's no 's' on njs event
                            foreach (JSONNode n in node)
                            {
                                njsEventsCommonData.Add(V4CommonData.NJSEvent.GetFromJson(n));
                            }
                            break;
                        
                        // Deprecated
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
                        case "njsEvents":
                            foreach (JSONNode n in node)
                            {
                                map.NJSEvents.Add(V4NJSEvent.GetFromJson(n, njsEventsCommonData));
                            }

                            break;
                        
                        // Deprecated
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
                map.NJSEvents.Sort();

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
            var filePath = Path.Combine(info.Directory, info.AudioDataFilename);
            if (!File.Exists(filePath))
            {
                Debug.Log($"No AudioData found at {filePath}");
                return;
            }
            
            var bpmInfo = V4AudioData.GetFromJson(BeatSaberSongUtils.GetNodeFromFile(filePath));

            var bpmEvents = BaseBpmInfo.GetBpmEvents(bpmInfo.BpmRegions, bpmInfo.AudioFrequency);
            map.BpmEvents = bpmEvents;
            map.BootstrapBpmEvents(info.BeatsPerMinute);
        }

        public static void LoadBookmarksFromOfficialEditor(BaseDifficulty map, BaseInfo info, InfoDifficulty infoDifficulty)
        {
            var bookmarksFolder = Path.Combine(info.Directory, "Bookmarks");
            var bookmarkFilePath = Path.Combine(bookmarksFolder, infoDifficulty.BookmarkFileName);
            if (!File.Exists(bookmarkFilePath))
            {
                return;
            }
            
            var node = BeatSaberSongUtils.GetNodeFromFile(Path.Combine(bookmarksFolder, bookmarkFilePath));
            if (!node["bookmarks"].IsArray)
            {
                return;
            }
                
            var color = node["color"].ReadHtmlStringColor();
            var bookmarks = node["bookmarks"].AsArray.Children
                .Select(jsonNode => jsonNode.AsObject)
                .Select(jsonObj => new BaseBookmark
                {
                    JsonTime = jsonObj["beat"].AsFloat,
                    Name = jsonObj["text"].Value,
                    Color = color
                })
                .ToList();

            map.Bookmarks = bookmarks;
        }
        
        public static void LoadLightsFromLightshowFile(BaseDifficulty map, BaseInfo info, InfoDifficulty infoDifficulty)
        {
            var filePath = Path.Combine(info.Directory, infoDifficulty.LightshowFileName);
            if (!File.Exists(filePath))
            {
                Debug.Log($"No lightshow file found at {filePath}");
                return;
            }
            
            var mainNode = BeatSaberSongUtils.GetNodeFromFile(filePath);

            LoadLightsFromJson(map, mainNode);
        }

        public static void LoadLightsFromJson(BaseDifficulty map, JSONNode mainNode)
        {
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
                    case "colorBoostEventsData":
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
                            lightColorEventBoxesCommonData.Add(V4CommonData.LightColorEventBox.GetFromJson(n));
                        }

                        break;
                    case "lightColorEvents":
                        foreach (JSONNode n in node)
                        {
                            lightColorEventsCommonData.Add(V4CommonData.LightColorEvent.GetFromJson(n));
                        }

                        break;
                    case "lightRotationEventBoxes":
                        foreach (JSONNode n in node)
                        {
                            lightRotationEventBoxesCommonData.Add(V4CommonData.LightRotationEventBox.GetFromJson(n));
                        }

                        break;
                    case "lightRotationEvents":
                        foreach (JSONNode n in node)
                        {
                            lightRotationEventsCommonData.Add(V4CommonData.LightRotationEvent.GetFromJson(n));
                        }

                        break;
                    case "lightTranslationEventBoxes":
                        foreach (JSONNode n in node)
                        {
                            lightTranslationEventBoxesCommonData.Add(V4CommonData.LightTranslationEventBox.GetFromJson(n));
                        }

                        break;
                    case "lightTranslationEvents":
                        foreach (JSONNode n in node)
                        {
                            lightTranslationEventsCommonData.Add(V4CommonData.LightTranslationEvent.GetFromJson(n));
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

            // List of empty lighting data - need this so we don't touch the map existing lights if an exception occurs
            var events = new List<BaseEvent>();
            var lightColorEventBoxGroups = new List<BaseLightColorEventBoxGroup<BaseLightColorEventBox>>();
            var lightRotationEventBoxGroups = new List<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>();
            var lightTranslationEventBoxGroups = new List<BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>>();
            var vfxEventBoxGroups = new List<BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>>();
            var waypoints = new List<BaseWaypoint>();
            var eventTypesWithKeywords = new BaseEventTypesWithKeywords();
            var useNormalEventsAsCompatibleEvents = true;
            
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
                            events.Add(V4BasicEvent.GetFromJson(n, basicEventsCommonData));
                        }

                        break;
                    
                    case "colorBoostEvents":
                        foreach (JSONNode n in node)
                        {
                            events.Add(V4ColorBoostEvent.GetFromJson(n, colorBoostEventsCommonData));
                        }

                        break;
                    
                    // Pain
                    case "eventBoxGroups":
                        foreach (JSONNode n in node)
                        {
                            var type = n["t"].AsInt;
                            switch (type)
                            {
                                case 1: // Light Color
                                    lightColorEventBoxGroups.Add(V4LightColorEventBoxGroup.GetFromJson(n,
                                        indexFilters, lightColorEventBoxesCommonData, lightColorEventsCommonData));
                                    break;
                                case 2: // Light Rotation
                                    lightRotationEventBoxGroups.Add(V4LightRotationEventBoxGroup.GetFromJson(n,
                                        indexFilters, lightRotationEventBoxesCommonData,
                                        lightRotationEventsCommonData));
                                    break;
                                case 3: // Light Translation
                                    lightTranslationEventBoxGroups.Add(V4LightTranslationEventBoxGroup.GetFromJson(
                                        n, indexFilters, lightTranslationEventBoxesCommonData,
                                        lightTranslationEventsCommonData));
                                    break;
                                case 4: // FX Events
                                    vfxEventBoxGroups.Add(V4VfxEventEventBoxGroup.GetFromJson(n, indexFilters,
                                        fxEventBoxesCommonData, floatFxEventsCommonData));
                                    break;
                            }
                        }
                    
                        break;
                    
                    case "waypoints":
                        foreach (JSONNode n in node)
                        {
                            waypoints.Add(V4Waypoint.GetFromJson(n, waypointsCommonData)); 
                        }
                        break;
                    
                    case "basicEventTypesWithKeywords":
                        eventTypesWithKeywords = V4BasicEventTypesWithKeywords.GetFromJson(node);
                        break;
                    
                    case "useNormalEventsAsCompatibleEvents":
                        useNormalEventsAsCompatibleEvents = node.AsBool;
                        break;
                }
            }

            // Finally replace all the lighting data with what we parsed
            map.Events = events;
            map.LightColorEventBoxGroups = lightColorEventBoxGroups;
            map.LightRotationEventBoxGroups = lightRotationEventBoxGroups;
            map.LightTranslationEventBoxGroups = lightTranslationEventBoxGroups;
            map.VfxEventBoxGroups = vfxEventBoxGroups;
            map.Waypoints = waypoints;
            map.EventTypesWithKeywords = eventTypesWithKeywords;
            map.UseNormalEventsAsCompatibleEvents = useNormalEventsAsCompatibleEvents;
            
            // Important!
            map.Events.Sort();
        }
    }
}
