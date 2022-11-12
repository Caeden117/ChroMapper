namespace Beatmap.Base
{
    public abstract class BaseIndexFilter : BaseItem
    {
        protected BaseIndexFilter()
        {
        }

        protected BaseIndexFilter(int type, int param0, int param1, int reverse, int chunks = 0, float limit = 0,
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

        public int Type { get; set; }
        public int Param0 { get; set; }
        public int Param1 { get; set; }
        public int Reverse { get; set; }
        public int Chunks { get; set; }
        public int Random { get; set; }
        public int Seed { get; set; }
        public float Limit { get; set; }
        public int LimitAffectsType { get; set; }
    }
}
