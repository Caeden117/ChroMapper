using System.Linq;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseEventTypesWithKeywords : BaseItem
    {
        public BaseEventTypesWithKeywords()
        {
        }

        protected BaseEventTypesWithKeywords(BaseEventTypesWithKeywords other)
        {
            if (other != null)
                Keywords = other.Keywords.Select(x => x.Clone() as BaseEventTypesForKeywords).ToArray();
        }

        protected BaseEventTypesWithKeywords(BaseEventTypesForKeywords[] keywords) => Keywords = keywords;

        public BaseEventTypesForKeywords[] Keywords { get; set; } = { };
        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2SpecialEventsKeywordFilters.ToJson(this),
            3 => V3BasicEventTypesWithKeywords.ToJson(this)
        };

        public override BaseItem Clone() => new BaseEventTypesWithKeywords(this);
    }
}
