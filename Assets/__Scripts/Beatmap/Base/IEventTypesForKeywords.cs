using System.Linq;

namespace Beatmap.Base
{
    public abstract class IEventTypesForKeywords : IItem
    {
        protected IEventTypesForKeywords()
        {
        }

        protected IEventTypesForKeywords(IEventTypesForKeywords other)
        {
            Keyword = other.Keyword;
            Events = other.Events.Select(x => x).ToArray();
        }

        protected IEventTypesForKeywords(string keyword, int[] events)
        {
            Keyword = keyword;
            Events = events;
        }

        public string Keyword { get; set; }
        public int[] Events { get; set; }
    }
}
