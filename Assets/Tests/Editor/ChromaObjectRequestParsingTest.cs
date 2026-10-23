using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Helper;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;

namespace Tests.Editor
{
    public class ChromaObjectRequestParsingTest : TestBase
    {
        [TestCase(2)]
        [TestCase(3)]
        public void NoteAndSliderFlagAliasesSurviveExportAndSuggestChroma(int version)
        {
            var previousVersion = Settings.Instance.MapVersion;
            var failures = new List<string>();
            try
            {
                Settings.Instance.MapVersion = version;
                foreach (var family in version == 2
                    ? new[] { "note", "bomb", "arc" }
                    : new[] { "note", "bomb", "arc", "chain" })
                {
                    foreach (var flagKey in new[] { "spawnEffect", "_disableSpawnEffect", "disableDebris" })
                    {
                        var customData = new JSONObject { [flagKey] = true };
                        var item = Parse(version, family, customData);
                        var output = item.ToJson().GetValueOrDefault(Key(version, "customData"), null);
                        var flag = output?.GetValueOrDefault(flagKey, null);
                        if (flag == null || !flag.IsBoolean || !flag.AsBool)
                            failures.Add($"V{version} {family}: accepted {flagKey}:true was lost during export.");

                        var map = new BaseDifficulty { Version = version == 2
                            ? "2.6.0"
                            : "3.2.0" };
                        switch (item)
                        {
                            case BaseNote note:
                                map.Notes.Add(note);
                                break;
                            case BaseArc arc:
                                map.Arcs.Add(arc);
                                break;
                            case BaseChain chain:
                                map.Chains.Add(chain);
                                break;
                        }

                        var info = new InfoDifficulty(new InfoDifficultySet());
                        if (new ChromaReq().IsRequiredOrSuggested(info, map) != RequirementCheck.RequirementType.Suggestion)
                            failures.Add($"V{version} {family}: accepted {flagKey} did not suggest Chroma.");

                    }
                }
            }

            finally
            {
                Settings.Instance.MapVersion = previousVersion;
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static BaseGrid Parse(int version, string family, JSONObject customData)
        {
            var data = family switch
            {
                "arc" when version == 2 => JSON.Parse("{\"_colorType\":0,\"_headTime\":10,\"_headLineIndex\":0,\"_headLineLayer\":0,\"_headCutDirection\":1,\"_headControlPointLengthMultiplier\":1,\"_tailTime\":12,\"_tailLineIndex\":0,\"_tailLineLayer\":1,\"_tailCutDirection\":1,\"_tailControlPointLengthMultiplier\":1,\"_sliderMidAnchorMode\":0}"),
                "arc" => JSON.Parse("{\"b\":10,\"c\":0,\"x\":0,\"y\":0,\"d\":1,\"mu\":1,\"tb\":12,\"tx\":0,\"ty\":1,\"tc\":1,\"tmu\":1,\"m\":0}"),
                "chain" => JSON.Parse("{\"b\":10,\"c\":1,\"x\":2,\"y\":0,\"d\":1,\"tb\":12,\"tx\":2,\"ty\":1,\"sc\":3,\"s\":1}"),
                "bomb" when version == 3 => JSON.Parse("{\"b\":10,\"x\":3,\"y\":0}"),
                _ when version == 2 => JSON.Parse("{\"_time\":10,\"_lineIndex\":0,\"_lineLayer\":0,\"_type\":0,\"_cutDirection\":1}"),
                _ => JSON.Parse("{\"b\":10,\"x\":0,\"y\":0,\"c\":0,\"d\":1}")
            };
            if (family == "bomb" && version == 2)
                data["_type"] = 3;

            data[Key(version, "customData")] = customData;
            return family switch
            {
                "arc" => BeatmapFactory.Arc(data),
                "chain" => BeatmapFactory.Chain(data),
                "bomb" => BeatmapFactory.Bomb(data),
                _ => BeatmapFactory.Note(data)
            };
        }

        private static string Key(int version, string name) => version == 2
            ? "_" + name
            : name;
    }
}
