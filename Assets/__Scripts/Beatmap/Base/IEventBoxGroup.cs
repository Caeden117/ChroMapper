using System.Collections.Generic;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class IEventBoxGroup<T> : IObject where T : IEventBox
    {
        protected IEventBoxGroup()
        {
        }

        protected IEventBoxGroup(float time, int id, List<T> events, JSONNode customData = null) : base(time,
            customData)
        {
            ID = id;
            Events = events;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public int ID { get; set; }
        public List<T> Events { get; set; } = new List<T>();

        public override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            if (other is IEventBoxGroup<T> eventBoxGroup) return ID == eventBoxGroup.ID;
            return false;
        }
    }
}
