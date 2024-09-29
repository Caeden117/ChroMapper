using System;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Info
{
    public static class V4AudioData
    {
        public const string FileName = "AudioData.dat";

        public static BaseBpmInfo GetFromJson(JSONNode node)
        {
            var bpmInfo = new BaseBpmInfo();

            bpmInfo.Version = node["version"];
            bpmInfo.AudioSamples = node["songSampleCount"];
            bpmInfo.AudioFrequency = node["songFrequency"];

            var bpmRegions = new List<BpmInfoBpmRegion>();
            foreach (JSONNode bpmData in node["bpmData"])
            {
                var bpmRegion = new BpmInfoBpmRegion
                {
                    StartSampleIndex = bpmData["si"],
                    EndSampleIndex = bpmData["ei"],
                    StartBeat = bpmData["sb"],
                    EndBeat = bpmData["eb"]
                };
                bpmRegions.Add(bpmRegion);
            }

            bpmInfo.BpmRegions = bpmRegions;

            var lufsRegions = new List<BpmInfoLufsRegion>();
            foreach (JSONNode lufsData in node["lufsData"])
            {
                var lufsRegion = new BpmInfoLufsRegion
                {
                    StartSampleIndex = lufsData["si"],
                    EndSampleIndex = lufsData["ei"],
                    Loudness = lufsData["l"]
                };
                lufsRegions.Add(lufsRegion);
            }

            bpmInfo.LufsRegions = lufsRegions;

            return bpmInfo;
        }

        public static JSONNode GetOutputJson(BaseBpmInfo bpmInfo)
        {
            JSONNode json = new JSONObject();
            
            json["version"] = bpmInfo.Version;
            json["songChecksum"] = ""; // Does this still exist on current version?
            json["songSampleCount"] = bpmInfo.AudioSamples;
            json["songFrequency"] = bpmInfo.AudioFrequency;
            
            var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var precision = Settings.Instance.BpmTimeValueDecimalPrecision;
            
            var bpmData = new JSONArray();
            
            if (bpmInfo.BpmRegions.Count == 0)
            {
                var audioLength = (float)bpmInfo.AudioSamples / bpmInfo.AudioFrequency;
                bpmData.Add(new JSONObject
                {
                    ["si"] = 0,
                    ["ei"] = bpmInfo.AudioSamples,
                    ["sb"] = 0f,
                    ["eb"] = new JSONNumberWithOverridenRounding((songBpm / 60f) * audioLength, precision),
                });
            }
            else
            {
                foreach (var bpmRegion in bpmInfo.BpmRegions)
                {
                    bpmData.Add(new JSONObject
                    {
                        ["si"] = bpmRegion.StartSampleIndex,
                        ["ei"] = bpmRegion.EndSampleIndex,
                        ["sb"] = new JSONNumberWithOverridenRounding(bpmRegion.StartBeat, precision),
                        ["eb"] = new JSONNumberWithOverridenRounding(bpmRegion.EndBeat, precision)
                    });
                }
            }
            
            json["bpmData"] = bpmData;

            var lufsData = new JSONArray();

            foreach (var lufsRegion in bpmInfo.LufsRegions)
            {
                lufsData.Add(new JSONObject
                {
                    ["si"] = lufsRegion.StartSampleIndex,
                    ["ei"] = lufsRegion.EndSampleIndex,
                    ["l"] = lufsRegion.Loudness
                });
            }

            json["lufsData"] = lufsData;
            
            return json;
        }
    }
}
