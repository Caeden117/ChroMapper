using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4Arc
    {
        public static BaseArc GetFromJson(JSONNode node, IList<V4CommonData.Note> notesCommonData,
            IList<V4CommonData.Arc> arcsCommonData)
        {
            var arc = new BaseArc();

            arc.JsonTime = node["hb"].AsFloat;
            arc.TailJsonTime = node["tb"].AsFloat;
            arc.Rotation = node["hr"].AsInt;
            arc.Rotation = node["tr"].AsInt;

            var headIndex = node["hi"].AsInt;
            var headNoteData = notesCommonData[headIndex];

            arc.PosX = headNoteData.PosX;
            arc.PosY = headNoteData.PosY;
            arc.Color = headNoteData.Color;
            arc.CutDirection = headNoteData.CutDirection;
            arc.AngleOffset = headNoteData.AngleOffset;
            
            var tailIndex = node["ti"].AsInt;
            var tailNoteData = notesCommonData[tailIndex];

            arc.TailPosX = tailNoteData.PosX;
            arc.TailPosY = tailNoteData.PosY;
            arc.TailCutDirection = tailNoteData.CutDirection;
            
            var arcIndex = node["ai"].AsInt;
            var arcData = arcsCommonData[arcIndex];

            arc.HeadControlPointLengthMultiplier = arcData.HeadControlPointLengthMultiplier;
            arc.TailControlPointLengthMultiplier = arcData.TailControlPointLengthMultiplier;
            arc.MidAnchorMode = arcData.MidAnchorMode;

            return arc;
        }

        public static JSONNode ToJson(BaseArc arc, IList<V4CommonData.Note> notesCommonData,
            IList<V4CommonData.Arc> arcsCommonData)
        {
            JSONNode node = new JSONObject();
            node["hb"] = arc.JsonTime;
            node["tb"] = arc.TailJsonTime;
            node["hr"] = arc.Rotation;
            node["tr"] = arc.TailRotation;

            var headNoteData = V4CommonData.Note.FromBaseSliderHead(arc);
            node["hi"] = notesCommonData.IndexOf(headNoteData);

            var tailNoteData = V4CommonData.Note.FromBaseArcTail(arc);
            node["ti"] = notesCommonData.IndexOf(tailNoteData);

            var arcData = V4CommonData.Arc.FromBaseArc(arc);
            node["ai"] = arcsCommonData.IndexOf(arcData);

            return node;
        }
    }
}
