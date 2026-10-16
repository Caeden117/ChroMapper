using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Info
{
    public class InfoDifficultySet
    {
        private string characteristic;
        private string customCharacteristicIconImageFileName;

        // Characteristic names also form the generated beatmap and bookmark filenames.
        public string Characteristic
        {
            get => characteristic;
            set => characteristic = PathUtils.RequireFileName(value);
        }

        public List<InfoDifficulty> Difficulties { get; set; } = new();

        public JSONObject CustomData { get; set; } = new(); // Pretty much just for v2 SongCore parsing
        
        public string CustomCharacteristicLabel { get; set; }

        public string CustomCharacteristicIconImageFileName
        {
            get => customCharacteristicIconImageFileName;
            set => customCharacteristicIconImageFileName = PathUtils.RequireFileName(value);
        }
    }
}
