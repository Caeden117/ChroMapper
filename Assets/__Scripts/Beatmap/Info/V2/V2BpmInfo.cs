using System;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Info
{
    public static class V2BpmInfo
    {
        public const string FileName = "BPMInfo.dat";
        
        public static BaseBpmInfo GetFromJson(JSONNode node)
        {
            var bpmInfo = new BaseBpmInfo();

            bpmInfo.Version = node["_version"];
            bpmInfo.AudioSamples = node["_songSampleCount"];
            bpmInfo.AudioFrequency = node["_songFrequency"];

            var regions = new List<BpmInfoBpmRegion>();
            foreach (JSONNode region in node["_regions"])
            {
                var bpmRegion = new BpmInfoBpmRegion
                {
                    StartSampleIndex = region["_startSampleIndex"],
                    EndSampleIndex = region["_endSampleIndex"],
                    StartBeat = region["_startBeat"],
                    EndBeat = region["_endBeat"]
                };
                regions.Add(bpmRegion);
            }

            bpmInfo.BpmRegions = regions;
            bpmInfo.LufsRegions = new List<BpmInfoLufsRegion>();

            return bpmInfo;
        }
        
        public static JSONNode GetOutputJson(BaseBpmInfo bpmInfo)
        {
            JSONNode json = new JSONObject();
            
            json["_version"] = bpmInfo.Version;
            json["_songSampleCount"] = bpmInfo.AudioSamples;
            json["_songFrequency"] = bpmInfo.AudioFrequency;
            
            var songBpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            var precision = Settings.Instance.BpmTimeValueDecimalPrecision;
            
            var regions = new JSONArray();
            
            if (bpmInfo.BpmRegions.Count == 0)
            {
                var audioLength = (float)bpmInfo.AudioSamples / bpmInfo.AudioFrequency;
                regions.Add(new JSONObject
                {
                    ["_startSampleIndex"] = 0,
                    ["_endSampleIndex"] = bpmInfo.AudioSamples,
                    ["_startBeat"] = 0f,
                    ["_endBeat"] = new JSONNumberWithOverridenRounding((songBpm / 60f) * audioLength, precision),
                });
            }
            else
            {
                foreach (var bpmRegion in bpmInfo.BpmRegions)
                {
                    regions.Add(new JSONObject
                    {
                        ["_startSampleIndex"] = bpmRegion.StartSampleIndex,
                        ["_endSampleIndex"] = bpmRegion.EndSampleIndex,
                        ["_startBeat"] = new JSONNumberWithOverridenRounding(bpmRegion.StartBeat, precision),
                        ["_endBeat"] = new JSONNumberWithOverridenRounding(bpmRegion.EndBeat, precision)
                    });
                }
            }
            
            json["_regions"] = regions;
            
            return json;
        }
    }
}
