using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Note
    {
        public const string CustomKeyAnimation = "_animation";

        public const string CustomKeyTrack = "_track";

        public const string CustomKeyColor = "_color";

        public const string CustomKeyCoordinate = "_position";

        public const string CustomKeyWorldRotation = "_rotation";

        public const string CustomKeyLocalRotation = "_localRotation";

        public const string CustomKeySpawnEffect = "_disableSpawnEffect";

        public const string CustomKeyNoteJumpMovementSpeed = "_noteJumpMovementSpeed";

        public const string CustomKeyNoteJumpStartBeatOffset = "_noteJumpStartBeatOffset";

        public const string CustomKeyDirection = "_cutDirection";

        public static BaseNote GetFromJson(JSONNode node)
        {
            var note = new BaseNote();
            
            note.JsonTime = BaseItem.GetRequiredNode(node, "_time").AsFloat;
            note.PosX = BaseItem.GetRequiredNode(node, "_lineIndex").AsInt;
            note.PosY = BaseItem.GetRequiredNode(node, "_lineLayer").AsInt;
            note.Type = BaseItem.GetRequiredNode(node, "_type").AsInt;
            note.CutDirection = BaseItem.GetRequiredNode(node, "_cutDirection").AsInt;
            note.CustomData = node["_customData"];
            note.RefreshCustom();

            return note;
        }

        public static JSONNode ToJson(BaseNote note)
        {
            JSONNode node = new JSONObject();
            node["_time"] = note.JsonTime;
            node["_lineIndex"] = note.PosX;
            node["_lineLayer"] = note.PosY;
            node["_type"] = note.Type;
            node["_cutDirection"] = note.CutDirection;
            note.CustomData = note.SaveCustom();
            if (!note.CustomData.Children.Any()) return node;
            node["_customData"] = note.CustomData;
            return node;
        }
    }
}
