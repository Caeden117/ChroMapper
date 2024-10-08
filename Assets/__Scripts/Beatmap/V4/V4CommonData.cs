using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4CommonData
    {
        public struct Note
        {
            public int PosX { get; set; }
            public int PosY { get; set; }
            public int Color { get; set; }
            public int CutDirection { get; set; }
            public int AngleOffset { get; set; }
        }

        public static Note FromBaseNote(BaseNote baseNote) => new()
        {
            PosX = baseNote.PosX,
            PosY = baseNote.PosY,
            Color = baseNote.Color,
            CutDirection = baseNote.CutDirection,
            AngleOffset = baseNote.AngleOffset
        };
    }
}
