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
        // Patch timing only: object data must survive even when object loading is disabled in settings.
        public static JSONNode WithTempo(JSONNode node, TempoMap tempo)
        {
            var version = node[node.HasKey("version") ? "version" : "_version"].Value;
            if (version.StartsWith("4.")) return node;
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
            }
            else throw new InvalidDataException("Unsupported difficulty version: " + version);
            return node;
        }

        public static TempoMap Load(BaseInfo info, BaseDifficulty map, int samples, int frequency)
        {
            var path = PathUtils.Combine(info.Directory, BaseBpmInfo.GetOutputFileName(info.MajorVersion, info));
            List<BpmInfoBpmRegion> regions = null;
            if (File.Exists(path) && (info.MajorVersion == 4 || map == null || map.BpmEvents.Count == 0))
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
                if (map != null && map.BpmEvents.Count == 0 && map.BpmChanges.Count > 0)
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
            foreach (var difficulty in settings)
            {
                var path = PathUtils.Combine(info.Directory, difficulty.InfoDifficulty.BeatmapFileName);
                if (!File.Exists(path)) continue;
                if (difficulty.Map == null) throw new InvalidDataException("Cannot parse " + path);
                writes[path] = WithTempo(JSON.Parse(File.ReadAllText(path)), tempo).ToString(2);
            }

            var dataPath = PathUtils.Combine(info.Directory, BaseBpmInfo.GetOutputFileName(info.MajorVersion, info));
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
            var backup = PathUtils.Combine(info.Directory, ".tempo-backups", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backup);
            foreach (var original in originals.Where(o => o.Value != null))
                File.WriteAllText(PathUtils.Combine(backup, Path.GetFileName(original.Key)), original.Value);

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
                var map = difficulty.Map;
                if (map == null) continue;
                map.BpmChanges.Clear();
                if (map.CustomData != null)
                {
                    map.CustomData.Remove("_BPMChanges");
                    map.CustomData.Remove("_bpmChanges");
                }
                map.BpmEvents = tempo.ToEvents();
                map.BootstrapBpmEvents(info.BeatsPerMinute);
                map.RecomputeAllObjectSongBpmTimes();
            }
        }
    }
}
