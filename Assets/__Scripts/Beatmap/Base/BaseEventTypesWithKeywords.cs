using System.Linq;

namespace Beatmap.Base
{
    public abstract class BaseEventTypesWithKeywords : BaseItem
    {
        protected BaseEventTypesWithKeywords()
        {
        }

        protected BaseEventTypesWithKeywords(BaseEventTypesWithKeywords other)
        {
            if (other != null)
                Keywords = other.Keywords.Select(x => x.Clone() as BaseEventTypesForKeywords).ToArray();
        }

        protected BaseEventTypesWithKeywords(BaseEventTypesForKeywords[] keywords) => Keywords = keywords;

        public BaseEventTypesForKeywords[] Keywords { get; set; } = { };
    }
}
