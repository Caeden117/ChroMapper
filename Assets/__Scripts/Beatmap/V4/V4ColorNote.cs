using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4ColorNote
    {
        public static BaseNote GetFromJson(JSONNode node, IList<V4CommonData.Note> notesCommonData)
        {
            var note = new BaseNote();
            
            note.JsonTime = node["b"].AsFloat;

            var index = node["i"].AsInt;
            var noteData = notesCommonData[index];

            note.PosX = noteData.PosX;
            note.PosY = noteData.PosY;
            note.Color = noteData.Color;
            note.CutDirection = noteData.CutDirection;
            note.AngleOffset = noteData.AngleOffset;
            
            return note;
        }

        public static JSONNode ToJson(BaseNote note, IList<V4CommonData.Note> notesCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = note.JsonTime;
            node["r"] = 0;

            var data = V4CommonData.Note.FromBaseNote(note);
            node["i"] = notesCommonData.IndexOf(data);
            
            return node;
        }
    }
}
