using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4ColorNoteData
    {
        public static V4CommonData.Note GetFromJson(JSONNode node, bool customFake = false)
        {
            var note = new V4CommonData.Note();
            
            note.PosX = node["x"].AsInt;
            note.PosY = node["y"].AsInt;
            note.Color = node["c"].AsInt;
            note.CutDirection = node["d"].AsInt;
            note.AngleOffset = node["a"].AsInt;
            
            return note;
        }

        public static JSONNode ToJson(V4CommonData.Note note)
        {
            JSONNode node = new JSONObject();

            node["x"] = note.PosX;
            node["y"] = note.PosY;
            node["c"] = note.Color;
            node["d"] = note.CutDirection;
            node["a"] = note.AngleOffset;

            return node;
        }
    }
}
