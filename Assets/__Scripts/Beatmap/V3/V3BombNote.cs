using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3BombNote
    {
        public static BaseNote GetFromJson(JSONNode node, bool customFake = false)
        {
            var note = new BaseNote();
            
            note.JsonTime = node["b"].AsFloat;
            note.PosX = node["x"].AsInt;
            note.PosY = node["y"].AsInt;
            note.Type = (int)NoteType.Bomb;
            note.CustomData = node["customData"];

            note.CustomFake = customFake;
            return note;
        }

        public static JSONNode ToJson(BaseNote note)
        {
            JSONNode node = new JSONObject();
            node["b"] = note.JsonTime;
            node["x"] = note.PosX;
            node["y"] = note.PosY;
            note.CustomData = note.SaveCustom();
            if (!note.CustomData.Children.Any()) return node;
            node["customData"] = note.CustomData;
            return node;
        }
    }
}
