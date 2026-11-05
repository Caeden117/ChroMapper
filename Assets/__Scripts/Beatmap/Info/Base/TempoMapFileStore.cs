using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.Info
{
    public static class TempoMapFileStore
    {
        // Migrate legacy object timing once; official tempo edits retain stored object beats.
        public static JSONNode WithTempo(JSONNode node, TempoMap tempo, float songBpm = 0)
        {
            var version = node[node.HasKey("version") ? "version" : "_version"].Value;
            var v4 = version.StartsWith("4.");
            if (!v4 && !version.StartsWith("2.") && !version.StartsWith("3."))
                throw new InvalidDataException("Unsupported difficulty version: " + version);
            if (v4) return node;
            var v2 = version.StartsWith("2.");
            var custom = node[v2 ? "_customData" : "customData"];
            var hasLegacy = v2
                ? node["_BPMChanges"].Count > 0 || node["_bpmChanges"].Count > 0 ||
                  custom["_BPMChanges"].Count > 0 || custom["_bpmChanges"].Count > 0
                : custom["BPMChanges"].Count > 0;
            if (hasLegacy && (songBpm <= 0 || float.IsNaN(songBpm) || float.IsInfinity(songBpm)))
                throw new InvalidDataException("The original song BPM is required to preserve object timing.");

            // Migrate against each difficulty's original legacy grid, before applying edits.
            // Read only BPM fields, independently of the global selected format and object-load settings.
            var targetEvents = new List<BaseBpmEvent>();
            if (hasLegacy)
            {
                var changes = new List<BaseBpmEvent>();
                void ReadChanges(JSONNode array)
                {
                    foreach (JSONNode change in array)
                    {
                        var timeKey = v2 ? "_time" : "b";
                        var bpmKey = v2 ? "_BPM" : "m";
                        if (!change.HasKey(timeKey) || !change.HasKey(bpmKey))
                            throw new InvalidDataException("Cannot read the original legacy BPM grid. No files were saved.");
                        changes.Add(new BaseBpmEvent(change[timeKey].AsFloat, change[bpmKey].AsFloat));
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
                targetEvents.Add(new BaseBpmEvent(0, songBpm));
                double previousTime = 0, previousBeat = 0, previousBpm = songBpm;
                foreach (var change in changes.GroupBy(change => change.JsonTime).Select(group => group.First())
                             .Where(change => change.JsonTime >= 0 && change.Bpm > 0)
                             .OrderBy(change => change.JsonTime))
                {
                    var beat = previousBeat + (change.JsonTime - previousTime) * previousBpm / songBpm;
                    var evt = new BaseBpmEvent((float)beat, change.Bpm);
                    if (targetEvents[targetEvents.Count - 1].JsonTime == evt.JsonTime)
                        targetEvents[targetEvents.Count - 1] = evt;
                    else targetEvents.Add(evt);
                    previousTime = change.JsonTime;
                    previousBeat = beat;
                    previousBpm = change.Bpm;
                }
            }
            var targetSeconds = new double[targetEvents.Count];
            for (var i = 1; i < targetSeconds.Length; i++)
                targetSeconds[i] = targetSeconds[i - 1] + (targetEvents[i].JsonTime - targetEvents[i - 1].JsonTime) * 60d / targetEvents[i - 1].Bpm;

            double BeatAtSeconds(double time)
            {
                var low = 0;
                var high = targetSeconds.Length - 1;
                while (low < high)
                {
                    var middle = (low + high + 1) / 2;
                    if (targetSeconds[middle] <= time) low = middle;
                    else high = middle - 1;
                }
                return targetEvents[low].JsonTime + (time - targetSeconds[low]) * targetEvents[low].Bpm / 60;
            }

            double Convert(double beat)
            {
                return BeatAtSeconds(beat * 60 / songBpm);
            }
            if (hasLegacy)
            {
                // Distribution widths can be scaled exactly wherever this conversion is affine.
                // Keep every slope change, including those introduced by the target audio grid.
                var breaks = targetSeconds.Skip(1).Select(time => time * songBpm / 60).ToList();
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

        public static TempoMap Load(BaseInfo info, BaseDifficulty map, int samples, int frequency)
        {
            var path = PathUtils.Combine(info.Directory, BaseBpmInfo.GetOutputFileName(info.MajorVersion, info));
            List<BpmInfoBpmRegion> regions = null;
            if (File.Exists(path) && (map == null ||
                    (map.BpmChanges.Count == 0 && (info.MajorVersion == 4 || map.BpmEvents.Count == 0))))
            {
                var node = JSON.Parse(File.ReadAllText(path));
                var data = info.MajorVersion == 4 ? V4AudioData.GetFromJson(node) : V2BpmInfo.GetFromJson(node);
                if (data.AudioFrequency != frequency || Math.Abs(data.AudioSamples - samples) > 1)
                    throw new InvalidDataException("Audio sample count/frequency differs from the saved tempo map.");
                regions = data.BpmRegions;
                // Accept the official editor's one-sample-separated endpoints into our shared-endpoint model.
                for (var i = 0; i < regions.Count - 1; i++)
                {
                    var r = regions[i];
                    if (regions[i + 1].StartSampleIndex - r.EndSampleIndex == 1)
                        r.EndSampleIndex = regions[i + 1].StartSampleIndex;
                    regions[i] = r;
                }
            }
            if (regions == null)
            {
                if (map != null && map.BpmChanges.Count > 0)
                {
                    // Legacy custom BPM timestamps are in the original song-BPM grid, rather than the changing beat grid.
                    var changes = map.BpmChanges.Where(change => change.JsonTime >= 0 && change.Bpm > 0)
                        .OrderBy(change => change.JsonTime).ToList();
                    regions = new List<BpmInfoBpmRegion>();
                    var start = 0;
                    var beat = 0f;
                    var bpm = info.BeatsPerMinute;
                    foreach (var change in changes)
                    {
                        var end = (int)Math.Round(change.JsonTime * frequency * 60d / info.BeatsPerMinute);
                        if (end >= samples) break;
                        if (end > start)
                        {
                            var endBeat = beat + (end - start) * bpm / (frequency * 60f);
                            regions.Add(new BpmInfoBpmRegion { StartSampleIndex = start, EndSampleIndex = end, StartBeat = beat, EndBeat = endBeat });
                            start = end;
                            beat = endBeat;
                        }
                        bpm = change.Bpm;
                    }
                    regions.Add(new BpmInfoBpmRegion { StartSampleIndex = start, EndSampleIndex = samples,
                        StartBeat = beat, EndBeat = beat + (samples - start) * bpm / (frequency * 60f) });
                }
                else
                {
                    map?.BootstrapBpmEvents(info.BeatsPerMinute);
                    regions = BaseBpmInfo.GetBpmInfoRegions(map?.BpmEvents ?? new List<BaseBpmEvent>(),
                        info.BeatsPerMinute, samples, frequency);
                }
            }
            return new TempoMap(regions, frequency);
        }

        public static void Save(BaseInfo info, TempoMap tempo, IEnumerable<DifficultySettings> difficulties)
        {
            tempo.Validate();
            var settings = difficulties.ToList();
            var writes = new Dictionary<string, string>();
            var dataPath = PathUtils.Combine(info.Directory, BaseBpmInfo.GetOutputFileName(info.MajorVersion, info));
            void Prepare(string path)
            {
                if (writes.ContainsKey(path) || !File.Exists(path)) return;
                writes[path] = WithTempo(JSON.Parse(File.ReadAllText(path)), tempo, info.BeatsPerMinute).ToString(2);
            }
            foreach (var difficulty in settings)
            {
                var path = PathUtils.Combine(info.Directory, difficulty.InfoDifficulty.BeatmapFileName);
                if (!File.Exists(path)) continue;
                if (difficulty.Map == null) throw new InvalidDataException("Cannot parse " + path);
                // V4 object files retain stored timestamps; only AudioData contains its tempo grid.
                if (difficulty.Map.MajorVersion != 4) Prepare(path);
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
            writes[dataPath] = output.ToString(2);

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
                // Reload on next access to use the saved official grid and any migrated object timestamps.
                difficulty.InvalidateMap();
            }
        }
    }
}
