using System.Linq;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseEventTypesForKeywords : BaseItem
    {
        public BaseEventTypesForKeywords()
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

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2SpecialEventsKeywordFiltersKeywords.ToJson(this),
            3 => V3BasicEventTypesForKeywords.ToJson(this)
        };

        public override BaseItem Clone() => new BaseEventTypesForKeywords(this);
    }
}
