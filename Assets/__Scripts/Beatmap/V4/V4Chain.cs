using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4Chain
    {
        public static BaseChain GetFromJson(JSONNode node, IList<V4CommonData.Note> notesCommonData,
            IList<V4CommonData.Chain> chainsCommonData)
        {
            var chain = new BaseChain();

            chain.JsonTime = node["hb"].AsFloat;
            chain.TailJsonTime = node["tb"].AsFloat;

            var headIndex = node["i"].AsInt;
            var headNoteData = notesCommonData[headIndex];

            chain.PosX = headNoteData.PosX;
            chain.PosY = headNoteData.PosY;
            chain.Color = headNoteData.Color;
            chain.CutDirection = headNoteData.CutDirection;
            chain.AngleOffset = headNoteData.AngleOffset;

            var chainIndex = node["ci"].AsInt;
            var chainData = chainsCommonData[chainIndex];

            chain.TailPosX = chainData.TailPosX;
            chain.TailPosY = chainData.TailPosY;
            chain.SliceCount = chainData.SliceCount;
            chain.Squish = chainData.Squish;

            return chain;
        }

        public static JSONNode ToJson(BaseChain chain, IList<V4CommonData.Note> notesCommonData,
            IList<V4CommonData.Chain> chainsCommonData)
        {
            JSONNode node = new JSONObject();
            node["hb"] = chain.JsonTime;
            node["tb"] = chain.TailJsonTime;
            node["hr"] = 0;
            node["tr"] = 0;

            var headNoteData = V4CommonData.Note.FromBaseSliderHead(chain);
            node["i"] = notesCommonData.IndexOf(headNoteData);

            var chainData = V4CommonData.Chain.FromBaseChain(chain);
            node["ci"] = chainsCommonData.IndexOf(chainData);

            return node;
        }
    }
}
