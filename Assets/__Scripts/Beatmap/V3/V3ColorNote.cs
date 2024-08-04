using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3ColorNote
    {
        public const string CustomKeyAnimation = "animation";

        public const string CustomKeyTrack = "track";

        public const string CustomKeyColor = "color";

        public const string CustomKeyCoordinate = "coordinates";

        public const string CustomKeyWorldRotation = "worldRotation";

        public const string CustomKeyLocalRotation = "localRotation";

        public const string CustomKeySpawnEffect = "spawnEffect";

        public const string CustomKeyNoteJumpMovementSpeed = "noteJumpMovementSpeed";

        public const string CustomKeyNoteJumpStartBeatOffset = "noteJumpStartBeatOffset";

        public const string CustomKeyDirection = "cutDirection";

        public static BaseNote GetFromJson(JSONNode node, bool? customFake = false)
        {
            var note = new BaseNote();
            
            note.JsonTime = node["b"].AsFloat;
            note.PosX = node["x"].AsInt;
            note.PosY = node["y"].AsInt;
            note.AngleOffset = node["a"].AsInt;
            note.Color = node["c"].AsInt;
            note.CutDirection = node["d"].AsInt;
            note.CustomData = node["customData"];
            note.RefreshCustom();

            if (customFake != null) note.CustomFake = customFake.Value;

            return note;
        }

        public static JSONNode ToJson(BaseNote note)
        {
            JSONNode node = new JSONObject();
            node["b"] = note.JsonTime;
            node["x"] = note.PosX;
            node["y"] = note.PosY;
            node["a"] = note.AngleOffset;
            node["c"] = note.Color;
            node["d"] = note.CutDirection;
            note.CustomData = note.SaveCustom();
            if (!note.CustomData.Children.Any()) return node;
            node["customData"] = note.CustomData;
            return node;
        }
    }
}
