using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseIndexFilter : BaseItem
    {
        public BaseIndexFilter()
        {
        }

        public BaseIndexFilter(int type, int param0, int param1, int reverse, int chunks = 0, float limit = 0,
            int limitAffectsType = 0, int random = 0, int seed = 0)
        {
            Type = type;
            Param0 = param0;
            Param1 = param1;
            Reverse = reverse;
            Chunks = chunks;
            Random = random;
            Seed = seed;
            Limit = limit;
            LimitAffectsType = limitAffectsType;
        }

        public BaseIndexFilter(BaseIndexFilter other)
        {
            Type = other.Type;
            Param0 = other.Param0;
            Param1 = other.Param1;
            Reverse = other.Reverse;
            Chunks = other.Chunks;
            Random = other.Random;
            Seed = other.Seed;
            Limit = other.Limit;
            LimitAffectsType = other.LimitAffectsType;
        }
        

        public int Type { get; set; }
        public int Param0 { get; set; }
        public int Param1 { get; set; }
        public int Reverse { get; set; }
        public int Chunks { get; set; }
        public int Random { get; set; }
        public int Seed { get; set; }
        public float Limit { get; set; }
        public int LimitAffectsType { get; set; }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3IndexFilter.ToJson(this)
        };

        public override BaseItem Clone() => new BaseIndexFilter(this);
    }
}
