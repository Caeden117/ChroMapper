using System.Linq;

namespace Beatmap.Base
{
    public abstract class IEventTypesWithKeywords : IItem
    {
        protected IEventTypesWithKeywords()
        {
        }

        protected IEventTypesWithKeywords(IEventTypesWithKeywords other) => Keywords = other.Keywords.Select(x => x.Clone() as IEventTypesForKeywords).ToArray();

        protected IEventTypesWithKeywords(IEventTypesForKeywords[] keywords) => Keywords = keywords;

        public IEventTypesForKeywords[] Keywords { get; set; }
    }
}
