using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;
using Random = System.Random;

namespace Beatmap.Base.Customs
{
    public abstract class BaseBookmark : BaseObject
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

        protected BaseBookmark()
        {
        }

        protected BaseBookmark(BaseBookmark other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Name = other.Name;
            Color = other.Color;
        }

        protected BaseBookmark(JSONNode node) => InstantiateHelper(ref node);

        protected BaseBookmark(float time, string name) : base(time)
        {
            Name = name;
            Color = Color.HSVToRGB((float)rand.NextDouble(), 0.75f, 1);
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Bookmark;

        public string Name { get; set; } = "Invalid Bookmark";
        public Color Color { get; set; }

        public abstract string KeyTime { get; }
        public abstract string KeyName { get; }
        public abstract string KeyColor { get; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) => true;

        public override JSONNode ToJson() =>
            new JSONObject { [KeyTime] = JsonTime, [KeyName] = Name, [KeyColor] = Color };

        private void InstantiateHelper(ref JSONNode node)
        {
            JsonTime = node.HasKey(KeyTime) ? node[KeyTime].AsFloat : 0f;
            Name = node.HasKey(KeyName) ? node[KeyName].Value : "Missing Name";
            Color = node.HasKey(KeyColor)
                ? node[KeyColor].ReadColor()
                : Color.HSVToRGB((float)rand.NextDouble(), 0.75f, 1);
        }
    }
}
