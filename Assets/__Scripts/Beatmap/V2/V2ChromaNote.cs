﻿using Beatmap.Base;
using Beatmap.Enums;

// ChromaToggle is dead. Probably should remove this.
namespace Beatmap.V2
{
    public class V2ChromaNote : BaseNote
    {
        public const int Monochrome = (int)NoteCutDirection.Up;
        public const int Bidirectional = (int)NoteCutDirection.Left;
        public const int Duochrome = (int)NoteCutDirection.Right;
        public const int HotGarbage = (int)NoteCutDirection.DownRight;
        public const int Alternate = (int)NoteCutDirection.Down;
        public const int Deflect = (int)NoteCutDirection.UpRight;

        public int BombRotation = 0;

        public BaseNote OriginalNote;

        public V2ChromaNote(BaseNote baseNote)
        {
            OriginalNote = baseNote;
            Type = baseNote.Type;
            CutDirection = baseNote.CutDirection;
            PosX = baseNote.PosX;
            PosY = baseNote.PosY;
            JsonTime = baseNote.JsonTime;
            Type = baseNote.Type;

            //Set custom JSON data here.
        }

        public override ObjectType ObjectType
        {
            get => ObjectType.CustomNote;
            set => base.ObjectType = value;
        }

        /*public new JSONNode ConvertToJSON() //Uncomment this when Custom JSON Data is ready.
        {
            return ConvertToNote().ConvertToJSON();
        }*/
    }
}
