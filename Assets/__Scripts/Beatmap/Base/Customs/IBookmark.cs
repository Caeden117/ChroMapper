using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;
using Random = System.Random;

namespace Beatmap.Base.Customs
{
    public abstract class IBookmark : IObject
    {
        private static readonly Random Rand = new Random();

        protected IBookmark()
        {
        }

        protected IBookmark(JSONNode node) => InstantiateHelper(ref node);

        protected IBookmark(float time, string name) : base(time)
        {
            Name = name;
            Color = Color.HSVToRGB((float)Rand.NextDouble(), 0.75f, 1);
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.BpmChange;

        public string Name { get; set; } = "Invalid Bookmark";
        public Color Color { get; set; }

        public abstract string KeyTime { get; }
        public abstract string KeyName { get; }
        public abstract string KeyColor { get; }

        protected override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false) => true;

        public override JSONNode ToJson() =>
            new JSONObject
            {
                [KeyTime] = Time,
                [KeyName] = Name,
                [KeyColor] = Color
            };

        protected void InstantiateHelper(ref JSONNode node)
        {
            Time = node[KeyTime]?.AsFloat ?? 0f;
            Name = node[KeyName]?.Value ?? "Missing Name";
            Color = node.HasKey(KeyColor) ? node[KeyColor].ReadColor() : Color.HSVToRGB((float)Rand.NextDouble(), 0.75f, 1);
        }
    }
}
