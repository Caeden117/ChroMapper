using Beatmap.Enums;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;
using Random = System.Random;

namespace Beatmap.Base.Customs
{
    public class BaseBookmark : BaseObject
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Name);
            writer.Put(Color.r);
            writer.Put(Color.g);
            writer.Put(Color.b);
            writer.Put(Color.a);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Name = reader.GetString();
            var r = reader.GetFloat();
            var g = reader.GetFloat();
            var b = reader.GetFloat();
            var a = reader.GetFloat();
            Color = new Color(r, g, b, a);
            base.Deserialize(reader);
        }

        private static readonly Random rand = new Random();
        
        private static Color NextRandomColor() => Color.HSVToRGB((float)rand.NextDouble(), 0.75f, 1);

        public BaseBookmark()
        {
        }

        protected BaseBookmark(BaseBookmark other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Name = other.Name;
            Color = other.Color;
        }

        public BaseBookmark(JSONNode node)
        {
            JsonTime = node.HasKey(KeyTime) ? node[KeyTime].AsFloat : 0f;
            Name = node.HasKey(KeyName) ? node[KeyName].Value : "Missing Name";
            Color = node.HasKey(KeyColor)
                ? node[KeyColor].ReadColor()
                : NextRandomColor();
        }

        public BaseBookmark(float time, string name) : base(time)
        {
            Name = name;
            Color = NextRandomColor();
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Bookmark;

        public string Name { get; set; } = "Invalid Bookmark";
        public Color Color { get; set; }

        public string KeyTime => Settings.Instance.MapVersion switch
        {
            2 => V2Bookmark.KeyTime,
            3 => V3Bookmark.KeyTime
        };

        public string KeyName => Settings.Instance.MapVersion switch
        {
            2 => V2Bookmark.KeyName,
            3 => V3Bookmark.KeyName
        };

        public string KeyColor => Settings.Instance.MapVersion switch
        {
            2 => V2Bookmark.KeyColor,
            3 => V3Bookmark.KeyColor
        };

        public override string CustomKeyTrack { get; } = "unusedTrack";
        public override string CustomKeyColor { get; } = "unusedColor";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) => true;

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2Bookmark.ToJson(this),
            3 or 4 => V3Bookmark.ToJson(this)
        };

        public override BaseItem Clone() => new BaseBookmark(this);
    }
}
