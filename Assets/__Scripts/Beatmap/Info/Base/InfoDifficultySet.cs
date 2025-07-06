using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Info
{
    public class InfoDifficultySet
    {
        public string Characteristic { get; set; }
        public List<InfoDifficulty> Difficulties { get; set; } = new();

        public JSONObject CustomData { get; set; } = new(); // Pretty much just for v2 SongCore parsing
        
        public string CustomCharacteristicLabel { get; set; }
        public string CustomCharacteristicIconImageFileName { get; set; }
    }
}