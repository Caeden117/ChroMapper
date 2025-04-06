using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseNJSEvent : BaseObject
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(UsePrevious);
            writer.Put(Easing);
            writer.Put(RelativeNJS);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            UsePrevious = reader.GetInt();
            Easing = reader.GetInt();
            RelativeNJS = reader.GetFloat();
            base.Deserialize(reader);
        }

        public BaseNJSEvent()
        {
        }

        public BaseNJSEvent(BaseNJSEvent other)
        {
            SetTimes(other.JsonTime);
            UsePrevious = other.UsePrevious;
            Easing = other.Easing;
            RelativeNJS = other.RelativeNJS;
            CustomData = other.SaveCustom().Clone();
        }
        
        // Used for Node Editor - Json names can be arbitrary now as it's not used in file io 
        public BaseNJSEvent(JSONNode node) // : this(BeatmapFactory.NJSEvent(node))
        {
            JsonTime = node["beat"].AsFloat;
            UsePrevious = node["usePrevious"].AsInt;
            Easing = node["easing"].AsInt;
            RelativeNJS = node["relative-njs"].AsInt;
            CustomData = node["customData"].AsObject;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.NJSEvent;

        public int UsePrevious { get; set; }
        public int Easing { get; set; }
        public float RelativeNJS { get; set; }


        public override string CustomKeyColor { get; } = "unusedColor";

        public override string CustomKeyTrack { get; } = "unusedTrack";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseNJSEvent njsEvent)
            {
                return Mathf.Approximately(njsEvent.JsonTime, JsonTime);
            }

            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseNJSEvent obs)
            {
                UsePrevious = obs.UsePrevious;
                Easing = obs.Easing;
                RelativeNJS = obs.RelativeNJS;
            }
        }

        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseNJSEvent njsEvent) return comparison;

            // Early return if use previous is 1 in both
            // Why: relative NJS and easing are irrelevant if this is set to 1, so these events can be considered equal
            if (UsePrevious == 1 && njsEvent.UsePrevious == 1) return comparison;

            // Compare by relative NJS if times match
            if (comparison == 0) comparison = RelativeNJS.CompareTo(njsEvent.RelativeNJS);

            // Compare by easing if relative NJS matches
            if (comparison == 0) comparison = Easing.CompareTo(njsEvent.Easing);

            // Compare by use previous if easing matches
            if (comparison == 0) comparison = UsePrevious.CompareTo(njsEvent.UsePrevious);

            return comparison;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            _ => new JSONObject
            {
                ["beat"] = JsonTime,
                ["usePrevious"] = UsePrevious,
                ["easing"] = Easing,
                ["relative-njs"] = RelativeNJS,
                // ["customData"] = CustomData,
            }
        };

        public override BaseItem Clone()
        {
            var evt = new BaseNJSEvent(this);
            evt.ParseCustom();
            
            return evt;
        }
    }
}
