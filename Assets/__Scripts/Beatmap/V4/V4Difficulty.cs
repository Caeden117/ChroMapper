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
                var bombNotes = new JSONArray();

                var notes = difficulty.Notes.Where(x => x.Type != (int)NoteType.Bomb).ToList();
                //var bombs = difficulty.Notes.Where(x => x.Type == (int)NoteType.Bomb).ToList();

                var commonNoteData = notes.Select(V4CommonData.FromBaseNote).Distinct().ToList();
                
                foreach (var note in notes)
                {
                    colorNotes.Add(V4ColorNote.ToJson(note, commonNoteData));
                }

                foreach (var noteData in commonNoteData)
                {
                    colorNotesData.Add(V4ColorNoteData.ToJson(noteData));
                }

                json["colorNotes"] = colorNotes;
                json["colorNotesData"] = colorNotesData;

                // Do this before adding customData
                if (Settings.Instance.SaveWithoutDefaultValues)
                {
                    SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);
                }

                // var customDataJson = GetOutputCustomJsonData(difficulty);
                // if (customDataJson.Children.Any())
                //     json["customData"] = customDataJson;

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
                var noteCommonData = new List<V4CommonData.Note>();
                
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
                                noteCommonData.Add(V4ColorNoteData.GetFromJson(n));
                            }
                            break;
                    }
                }

                //noteCommonData = noteCommonData.Distinct().ToList();
                
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
                                map.Notes.Add(V4ColorNote.GetFromJson(n, noteCommonData));
                            }
                            break;
                    }
                }

                // LoadCustom(map, mainNode);

                // Important!
                map.Notes.Sort();
                map.Events.Sort();

                // Do not assume map is sorted for other things anyway
                map.Obstacles.Sort();
                map.Waypoints.Sort();
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

        // TODO: This is temporary just to see if the Info v4 stuff works.
        // TODO: I don't like how BeatSaberSongContainer Instance is being used here
        public static void LoadBpmFromAudioData(BaseDifficulty map)
        {
            var info = BeatSaberSongContainer.Instance.Info;
            var bpmInfo = V4AudioData.GetFromJson(BeatSaberSongUtils.GetNodeFromFile(Path.Combine(info.Directory, info.AudioDataFilename)));

            var bpmEvents = BaseBpmInfo.GetBpmEvents(bpmInfo.BpmRegions, bpmInfo.AudioFrequency);
            map.BpmEvents = bpmEvents;
        }

        private static void LoadLightsFromLightshowNode(BaseDifficulty map)
        {
            throw new NotImplementedException();
        }
    }
}
