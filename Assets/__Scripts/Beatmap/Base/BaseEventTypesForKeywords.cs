using System.Linq;

namespace Beatmap.Base
{
    public abstract class BaseEventTypesForKeywords : BaseItem
    {
        protected BaseEventTypesForKeywords()
        {
        }

        protected BaseEventTypesForKeywords(BaseEventTypesForKeywords other)
        {
            Keyword = other.Keyword;
            Events = other.Events.Select(x => x).ToArray();
        }

        protected BaseEventTypesForKeywords(string keyword, int[] events)
        {
            Keyword = keyword;
            Events = events;
        }

        public string Keyword { get; set; }
        public int[] Events { get; set; } = { };
    }
}
