﻿using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4BombNote
    {
        public static BaseNote GetFromJson(JSONNode node, IList<V4CommonData.Bomb> bombsCommonData)
        {
            var note = new BaseNote();
            
            note.JsonTime = node["b"].AsFloat;
            note.Rotation = node["r"].AsInt;

            var index = node["i"].AsInt;
            var bombData = bombsCommonData[index];

            note.PosX = bombData.PosX;
            note.PosY = bombData.PosY;
            note.Type = (int)NoteType.Bomb;
            
            return note;
        }

        public static JSONNode ToJson(BaseNote note, IList<V4CommonData.Bomb> bombsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = note.JsonTime;
            node["r"] = note.Rotation;

            var data = V4CommonData.Bomb.FromBaseNote(note);
            node["i"] = bombsCommonData.IndexOf(data);
            
            return node;
        }
    }
}