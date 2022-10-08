using Beatmap.Enums;
using Beatmap.Base;

namespace Beatmap.V2
{
    public class V2ChromaNote : V2Note
    {
        public const int Monochrome = (int)NoteCutDirection.Up;
        public const int Bidirectional = (int)NoteCutDirection.Left;
        public const int Duochrome = (int)NoteCutDirection.Right;
        public const int HotGarbage = (int)NoteCutDirection.DownRight;
        public const int Alternate = (int)NoteCutDirection.Down;
        public const int Deflect = (int)NoteCutDirection.UpRight;

        public int BombRotation = 0;

        public INote OriginalNote;

        public V2ChromaNote(INote note)
        {
            OriginalNote = note;
            Type = note.Type;
            CutDirection = note.CutDirection;
            PosX = note.PosX;
            PosY = note.PosY;
            Time = note.Time;
            Type = note.Type;

            //Set custom JSON data here.
        }

        public override ObjectType ObjectType
        {
            get => ObjectType.CustomNote;
            set => base.ObjectType = value;
        }

        public V2Note ConvertToNote() =>
            new V2Note(Time, PosX, PosY, Type, CutDirection, CustomData);

        /*public new JSONNode ConvertToJSON() //Uncomment this when Custom JSON Data is ready.
        {
            return ConvertToNote().ConvertToJSON();
        }*/
    }
}
