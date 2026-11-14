using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Info
{
    public static class TempoMapFileStore
    {
        // Migrate against the original grid before replacing the official events with the edited grid.
        public static JSONNode WithTempo(JSONNode node, TempoMap tempo, float songBpm = 0,
            JSONObject difficultyCustomData = null)
        {
            var version = node[node.HasKey("version") ? "version" : "_version"].Value;
            var v2 = version.StartsWith("2.");
            if (version.StartsWith("4.")) return node;
            if (!v2 && !version.StartsWith("3."))
                throw new InvalidDataException("Unsupported difficulty version: " + version);

            var changes = ReadLegacyChanges(node, v2);
            if (BaseDifficulty.HasLegacyTempo(changes, difficultyCustomData))
            {
                if (songBpm <= 0 || float.IsNaN(songBpm) || float.IsInfinity(songBpm))
                    throw new InvalidDataException("The original song BPM is required to migrate legacy tempo.");

                var steps = new List<(Func<double, double> Shift, Func<double, double> Scale)>();
                var breaks = new List<double>();
                BaseDifficulty.ConvertLegacyTempo(songBpm, changes, difficultyCustomData, new List<BaseBpmEvent>(),
                    (shift, scale) => steps.Add((shift, scale)), breaks);

                double Convert(double beat)
                {
                    foreach (var (shift, scale) in steps)
                    {
                        beat = shift(beat);
                        if (scale != null) beat = scale(beat);
                    }
                    return beat;
                }

                ConvertObjectTimes(node, v2, Convert, Convert, breaks);
            }
            var events = new JSONArray();
            if (version.StartsWith("2."))
            {
                foreach (JSONNode evt in node["_events"])
                    if (evt["_type"].AsInt != 100) events.Add(evt);
                foreach (var evt in tempo.ToEvents())
                    events.Add(new JSONObject
                    {
                        ["_time"] = evt.JsonTime, ["_type"] = 100, ["_value"] = 0, ["_floatValue"] = evt.Bpm
                    });
                var sorted = new JSONArray();
                foreach (var evt in events.Children.OrderBy(e => e["_time"].AsDouble)) sorted.Add(evt);
                node["_events"] = sorted;
                // Official tempo events replace obsolete editor-only grids. Keep other custom data untouched.
                node.Remove("_BPMChanges");
                node.Remove("_bpmChanges");
                if (node.HasKey("_customData"))
                {
                    node["_customData"].Remove("_BPMChanges");
                    node["_customData"].Remove("_bpmChanges");
                }
            }
            else if (version.StartsWith("3."))
            {
                foreach (var evt in tempo.ToEvents())
                    events.Add(new JSONObject { ["b"] = evt.JsonTime, ["m"] = evt.Bpm });
                node["bpmEvents"] = events;
                if (node.HasKey("customData")) node["customData"].Remove("BPMChanges");
            }
            else throw new InvalidDataException("Unsupported difficulty version: " + version);
            return node;
        }

        private static List<BaseBpmChange> ReadLegacyChanges(JSONNode node, bool v2)
        {
            var changes = new List<BaseBpmChange>();
            void ReadChanges(JSONNode array)
            {
                foreach (JSONNode change in array)
                {
                    var time = v2 ? "_time" : "b";
                    var bpm = v2 ? "_BPM" : "m";
                    if (!change.HasKey(time) || !change.HasKey(bpm))
                        throw new InvalidDataException("Cannot read the original legacy BPM grid. No files were saved.");
                    changes.Add(new BaseBpmChange { JsonTime = change[time].AsFloat, Bpm = change[bpm].AsFloat });
                }
            }
            foreach (var entry in node)
            {
                if (v2 && (entry.Key == "_BPMChanges" || entry.Key == "_bpmChanges")) ReadChanges(entry.Value);
                if (entry.Key != (v2 ? "_customData" : "customData")) continue;
                foreach (var field in entry.Value)
                    if (v2 ? field.Key == "_BPMChanges" || field.Key == "_bpmChanges" : field.Key == "BPMChanges")
                        ReadChanges(field.Value);
            }
            return changes.DistinctBy(change => change.JsonTime).ToList();
        }

        private static void ConvertObjectTimes(JSONNode node, bool v2, Func<double, double> convert,
            Func<double, double> convertSongBeat, List<double> breaks)
        {
            double Distribution(JSONNode box, double start, IEnumerable<double> offsets)
            {
                var width = box["w"].AsDouble;
                if (width == 0) return 0;
                var positions = offsets.ToList();
                if (positions.Count == 0) return width;
                // Step widths are per light; without the environment's light count their total span
                // is unbounded. Wave widths bound the span independently of the selected lights.
                var first = positions.Min();
                var last = positions.Max();
                var minimum = start + first + Math.Min(0, width);
                var maximum = start + last + Math.Max(0, width);
                if (box["d"].AsInt != 1)
                {
                    if (width > 0) maximum = double.PositiveInfinity;
                    else minimum = double.NegativeInfinity;
                }
                var slope = convert(start + first + 0.0001) - convert(start + first);
                foreach (var boundary in breaks)
                {
                    if (boundary <= minimum || boundary >= maximum) continue;
                    var before = convert(boundary) - convert(boundary - 0.0001);
                    var after = convert(boundary + 0.0001) - convert(boundary);
                    if (Math.Abs(before - after) > 0.00000001 || Math.Abs(after - slope) > 0.00000001)
                        throw new InvalidDataException("This lighting distribution crosses a tempo conversion boundary and cannot preserve every light's timing. No files were saved.");
                }
                return width * slope / 0.0001;
            }

            void Objects(JSONNode array, string time, string duration = null, string tail = null)
            {
                foreach (JSONNode obj in array)
                {
                    var start = obj[time].AsDouble;
                    if (duration != null && obj.HasKey(duration))
                        obj[duration] = convert(start + obj[duration].AsDouble) - convert(start);
                    if (tail != null && obj.HasKey(tail)) obj[tail] = convert(obj[tail].AsDouble);
                    obj[time] = convert(start);
                }
            }

            if (v2)
            {
                Objects(node["_notes"], "_time");
                Objects(node["_obstacles"], "_time", "_duration");
                Objects(node["_waypoints"], "_time");
                Objects(node["_sliders"], "_headTime", tail: "_tailTime");
                foreach (JSONNode evt in node["_events"])
                    if (evt["_type"].AsInt != 100) evt["_time"] = convert(evt["_time"].AsDouble);
                Objects(node["_customData"]["_customEvents"], "_time");
                Objects(node["_customEvents"], "_time");
                var custom = node["_customData"];
                var bookmarkConversion = custom["_bookmarksUseOfficialBpmEvents"].AsBool ? convert : convertSongBeat;
                foreach (var bookmarks in new[] { custom["_bookmarks"], node["_bookmarks"] })
                {
                    foreach (JSONNode bookmark in bookmarks)
                        bookmark["_time"] = bookmarkConversion(bookmark["_time"].AsDouble);
                }
                if (custom.HasKey("_bookmarks") || node.HasKey("_bookmarks"))
                    node["_customData"]["_bookmarksUseOfficialBpmEvents"] = true;
            }
            else
            {
                foreach (var key in new[] { "colorNotes", "bombNotes", "basicBeatmapEvents", "rotationEvents",
                             "colorBoostBeatmapEvents", "waypoints" })
                    Objects(node[key], "b");
                var floatEvents = node["_fxEventsCollection"]["_fl"];
                var convertedFx = new JSONArray();
                foreach (var key in new[] { "lightColorEventBoxGroups", "lightRotationEventBoxGroups",
                             "lightTranslationEventBoxGroups", "vfxEventBoxGroups" })
                {
                    foreach (JSONNode group in node[key])
                    {
                        var start = group["b"].AsDouble;
                        foreach (JSONNode box in group["e"])
                        {
                            var eventsKey = key == "lightColorEventBoxGroups" ? "e" : "l";
                            var events = box[eventsKey].Children.ToList();
                            var offsets = key == "vfxEventBoxGroups"
                                ? events.Select(evt => floatEvents[evt.AsInt]["b"].AsDouble)
                                : events.Select(evt => evt["b"].AsDouble);
                            var width = Distribution(box, start, offsets);
                            if (box.HasKey("w")) box["w"] = width;
                            if (key == "vfxEventBoxGroups")
                            {
                                // FX entries can be shared by groups at different beats. Give each reference
                                // its own converted offset rather than mutating another group's timing.
                                var references = box["l"];
                                for (var i = 0; i < references.Count; i++)
                                {
                                    var evt = floatEvents[references[i].AsInt].Clone();
                                    evt["b"] = convert(start + evt["b"].AsDouble) - convert(start);
                                    references[i] = convertedFx.Count;
                                    convertedFx.Add(evt);
                                }
                                continue;
                            }
                            foreach (var evt in events)
                                if (evt.HasKey("b")) evt["b"] = convert(start + evt["b"].AsDouble) - convert(start);
                        }
                        group["b"] = convert(start);
                    }
                }
                if (convertedFx.Count > 0) node["_fxEventsCollection"]["_fl"] = convertedFx;
                Objects(node["obstacles"], "b", "d");
                Objects(node["sliders"], "b", tail: "tb");
                Objects(node["burstSliders"], "b", tail: "tb");
                var custom = node["customData"];
                foreach (var key in new[] { "fakeColorNotes", "fakeBombNotes", "njsEvents" }) Objects(custom[key], "b");
                Objects(custom["fakeObstacles"], "b", "d");
                Objects(custom["fakeBurstSliders"], "b", tail: "tb");
                Objects(custom["customEvents"], "b");
                if (custom.HasKey("bookmarks"))
                {
                    var bookmarkConversion = custom["bookmarksUseOfficialBpmEvents"].AsBool ? convert : convertSongBeat;
                    foreach (JSONNode bookmark in custom["bookmarks"])
                        bookmark["b"] = bookmarkConversion(bookmark["b"].AsDouble);
                    custom["bookmarksUseOfficialBpmEvents"] = true;
                }
            }
        }

        private static TempoMap LoadSharedGrid(BaseInfo info, int samples, int frequency)
        {
            var path = PathUtils.Combine(info.Directory, BaseBpmInfo.GetOutputFileName(info.MajorVersion, info));
            if (!File.Exists(path)) return null;
            var node = JSON.Parse(File.ReadAllText(path));
            if (node == null) throw new InvalidDataException("Unable to read the saved tempo map.");
            var data = info.MajorVersion == 4 ? V4AudioData.GetFromJson(node) : V2BpmInfo.GetFromJson(node);

            var regions = data.BpmRegions;
            // Accept the official editor's one-sample-separated endpoints into our shared-endpoint model.
            for (var i = 0; i < regions.Count - 1; i++)
            {
                var r = regions[i];
                if (regions[i + 1].StartSampleIndex - r.EndSampleIndex == 1)
                    r.EndSampleIndex = regions[i + 1].StartSampleIndex;
                regions[i] = r;
            }
            var original = new TempoMap(regions, data.AudioFrequency);
            if (Math.Abs((long)original.EndSample - data.AudioSamples) > 1)
                throw new InvalidDataException("Saved tempo regions do not cover the original audio clip.");

            var events = BaseBpmInfo.GetBpmEvents(regions, data.AudioFrequency);
            if (data.AudioFrequency == frequency && Math.Abs((long)original.EndSample - samples) <= 1
                && Math.Abs((long)data.AudioSamples - samples) <= 1)
                return new TempoMap(regions, frequency, events);

            if (samples <= 0 || frequency <= 0) throw new ArgumentException("Invalid replacement audio.");

            // Keep anchors at their original elapsed times. The final surviving region covers the new tail.
            var recovered = new List<BpmInfoBpmRegion>();
            var recoveredEvents = new List<BaseBpmEvent>();
            for (var i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                var start = Math.Round(region.StartSampleIndex * (double)frequency / data.AudioFrequency);
                if (start >= samples) break;
                region.StartSampleIndex = (int)start;
                if (recovered.Count > 0 && recovered[^1].StartSampleIndex == region.StartSampleIndex)
                {
                    // Multiple anchors can land on the same sample at a lower rate.
                    if (region.StartSampleIndex == 0) region.StartBeat = 0;
                    recovered[^1] = region;
                    recoveredEvents[^1] = events[i];
                    continue;
                }
                recovered.Add(region);
                recoveredEvents.Add(events[i]);
            }

            for (var i = 0; i < recovered.Count; i++)
            {
                var region = recovered[i];
                region.EndSampleIndex = i + 1 < recovered.Count ? recovered[i + 1].StartSampleIndex : samples;
                region.EndBeat = i + 1 < recovered.Count ? recovered[i + 1].StartBeat :
                    region.StartBeat + (region.EndSampleIndex - region.StartSampleIndex) *
                    recoveredEvents[i].Bpm / (60f * frequency);
                recovered[i] = region;
            }
            return new TempoMap(recovered, frequency, recoveredEvents) { RecoveredFromAudioChange = true };
        }

        public static TempoMap LoadWithoutDifficulty(BaseInfo info, bool hasDifficulties, int samples, int frequency)
        {
            try
            {
                var shared = LoadSharedGrid(info, samples, frequency);
                if (shared != null) return shared;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                || exception is InvalidDataException || exception is JSONParseException || exception is ArgumentException)
            {
                // Unusable shared metadata cannot be the source for a song-wide edit.
            }

            return hasDifficulties ? null : FromEvents(info, new List<BaseBpmEvent>(), samples, frequency);
        }

        public static TempoMap Load(BaseInfo info, BaseDifficulty map, int samples, int frequency, JSONObject difficultyCustomData = null)
        {
            if (map == null) return LoadWithoutDifficulty(info, false, samples, frequency);
            if (map.MajorVersion != 4 && BaseDifficulty.HasLegacyTempo(map.BpmChanges, difficultyCustomData))
            {
                var migrated = new BaseDifficulty();
                var changes = map.BpmChanges.Select(change =>
                    new BaseBpmChange { JsonTime = change.JsonTime, Bpm = change.Bpm }).ToList();
                BaseDifficulty.ConvertLegacyTempo(info.BeatsPerMinute, changes,
                    difficultyCustomData?.Clone().AsObject, migrated.BpmEvents, (_, __) => { });
                map = migrated;
            }
            else if (info.MajorVersion == 4 || map.BpmEvents.Count == 0)
            {
                var shared = LoadSharedGrid(info, samples, frequency);
                if (shared != null) return shared;
            }

            map.BootstrapBpmEvents(info.BeatsPerMinute);
            return FromEvents(info, map.BpmEvents, samples, frequency);
        }

        private static TempoMap FromEvents(BaseInfo info, List<BaseBpmEvent> events, int samples, int frequency)
        {
            var visibleEvents = events.TakeWhile(evt => BaseBpmInfo.GetEventSample(evt,
                info.BeatsPerMinute, frequency) < samples).ToList();
            var regions = BaseBpmInfo.GetBpmInfoRegions(visibleEvents, info.BeatsPerMinute, samples, frequency);
            var sourceEvents = visibleEvents.Count > 0 ? visibleEvents : new List<BaseBpmEvent> { new(0, info.BeatsPerMinute) };
            return new TempoMap(regions, frequency, sourceEvents);
        }

        public static bool Save(BaseInfo info, TempoMap tempo, IEnumerable<DifficultySettings> difficulties)
        {
            tempo.Validate();
            var settings = difficulties.ToList();
            var writes = new Dictionary<string, string>();
            var dataPath = PathUtils.Combine(info.Directory, BaseBpmInfo.GetOutputFileName(info.MajorVersion, info));
            var formatJson = Settings.Instance.FormatJson;
            string Serialize(JSONNode json) => formatJson ? json.ToString(2) : json.ToString();

            var migrated = false;
            var migratedOffsets = new HashSet<string>();
            var offsets = new Dictionary<string, float>();
            foreach (var difficulty in settings)
            {
                var path = PathUtils.Combine(info.Directory, difficulty.InfoDifficulty.BeatmapFileName);
                if (!File.Exists(path)) continue;
                var map = difficulty.Map;
                if (map == null) throw new InvalidDataException("Cannot parse " + path);
                if (map.MajorVersion == 4) continue;

                var customData = difficulty.InfoDifficulty.CustomData?.Clone().AsObject;
                var offset = Math.Max(0, BaseDifficulty.GetLegacyEditorOffset(customData));
                if (offsets.TryGetValue(path, out var previousOffset))
                {
                    if (previousOffset != offset)
                        throw new InvalidDataException("A shared difficulty file has conflicting legacy editor offsets.");
                    continue;
                }
                offsets.Add(path, offset);
                migrated |= BaseDifficulty.HasLegacyTempo(map.BpmChanges, customData);
                if (offset > 0) migratedOffsets.Add(difficulty.InfoDifficulty.BeatmapFileName);
                writes[path] = Serialize(WithTempo(JSON.Parse(File.ReadAllText(path)), tempo, info.BeatsPerMinute, customData));
            }

            if (migratedOffsets.Count > 0)
            {
                // Update only the legacy fields in Info.dat, preserving unrelated on-disk metadata.
                var infoPath = PathUtils.Combine(info.Directory, "Info.dat");
                var infoNode = JSON.Parse(File.ReadAllText(infoPath));
                var v4 = info.MajorVersion == 4;
                var entries = v4 ? infoNode["difficultyBeatmaps"].Children :
                    infoNode["_difficultyBeatmapSets"].Children.SelectMany(set => set["_difficultyBeatmaps"].Children);
                foreach (var entry in entries)
                {
                    if (!migratedOffsets.Contains(entry[v4 ? "beatmapDataFilename" : "_beatmapFilename"].Value)) continue;
                    var custom = entry[v4 ? "customData" : "_customData"];
                    custom.Remove("_editorOffset");
                    custom.Remove("_editorOldOffset");
                }
                writes[infoPath] = Serialize(infoNode);
            }

            var data = new BaseBpmInfo
            {
                Version = info.MajorVersion == 4 ? "4.0.0" : "2.0.0",
                AudioFrequency = tempo.Frequency, AudioSamples = tempo.EndSample,
                BpmRegions = tempo.Regions.ToList()
            };
            var output = info.MajorVersion == 4 ? V4AudioData.GetOutputJson(data) : V2BpmInfo.GetOutputJson(data);
            // Preserve loudness, checksums and future fields in shared audio metadata.
            if (File.Exists(dataPath))
            {
                var existing = JSON.Parse(File.ReadAllText(dataPath));
                foreach (var key in new[] { "version", "songSampleCount", "songFrequency", "bpmData",
                             "_version", "_songSampleCount", "_songFrequency", "_regions" })
                    if (output.HasKey(key)) existing[key] = output[key];
                output = existing;
            }
            writes[dataPath] = Serialize(output);

            var originals = writes.Keys.ToDictionary(p => p, p => File.Exists(p) ? File.ReadAllText(p) : null);

            var completed = new List<string>();
            try
            {
                foreach (var write in writes)
                {
                    var temporary = write.Key + ".tempo-tmp";
                    try
                    {
                        File.WriteAllText(temporary, write.Value);
                        if (File.Exists(write.Key)) File.Replace(temporary, write.Key, null);
                        else File.Move(temporary, write.Key);
                        completed.Add(write.Key);
                    }
                    finally
                    {
                        if (File.Exists(temporary)) File.Delete(temporary);
                    }
                }
            }
            catch
            {
                foreach (var path in completed)
                {
                    if (originals[path] == null) File.Delete(path);
                    else File.WriteAllText(path, originals[path]);
                }
                throw;
            }

            foreach (var difficulty in settings)
            {
                if (migratedOffsets.Contains(difficulty.InfoDifficulty.BeatmapFileName))
                {
                    difficulty.InfoDifficulty.CustomData?.Remove("_editorOffset");
                    difficulty.InfoDifficulty.CustomData?.Remove("_editorOldOffset");
                }
                // Reload on next access to use the saved official grid and any migrated object timestamps.
                difficulty.InvalidateMap();
            }
            return migrated;
        }
    }
}
