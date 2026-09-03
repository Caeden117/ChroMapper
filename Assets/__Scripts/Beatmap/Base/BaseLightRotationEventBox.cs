using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationEventBox : BaseEventBox
    {
        public BaseLightRotationEventBox()
        {
            RotationDistributionType = (int)DistributionType.Wave;
            Events = Array.Empty<BaseLightRotationBase>();
        }

        protected BaseLightRotationEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float rotationDistribution,
            int rotationDistributionType,
            int rotationAffectFirst,
            int axis,
            int flip,
            BaseLightRotationBase[] events) : base(
            indexFilter,
            beatDistribution,
            beatDistributionType)
        {
            RotationDistribution = rotationDistribution;
            RotationDistributionType = rotationDistributionType;
            RotationAffectFirst = rotationAffectFirst;
            Axis = axis;
            Flip = flip;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightRotationEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float rotationDistribution,
            int rotationDistributionType,
            int rotationAffectFirst,
            int axis,
            int flip,
            int easing,
            BaseLightRotationBase[] events) : base(
            indexFilter,
            beatDistribution,
            beatDistributionType,
            easing)
        {
            RotationDistribution = rotationDistribution;
            RotationDistributionType = rotationDistributionType;
            RotationAffectFirst = rotationAffectFirst;
            Axis = axis;
            Flip = flip;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightRotationEventBox(BaseLightRotationEventBox other) : base(
            other.IndexFilter.Clone() as BaseIndexFilter,
            other.BeatDistribution,
            other.BeatDistributionType,
            other.Easing)
        {
            RotationDistribution = other.RotationDistribution;
            RotationDistributionType = other.RotationDistributionType;
            RotationAffectFirst = other.RotationAffectFirst;
            Axis = other.Axis;
            Flip = other.Flip;
            IsAutomaticAxisLane = other.IsAutomaticAxisLane;
            Events = other.Events.Select(x => x.Clone()).Cast<BaseLightRotationBase>().ToArray();
        }

        public float RotationDistribution { get; set; }
        public int RotationDistributionType { get; set; }
        public int RotationAffectFirst { get; set; }
        public int Axis { get; set; }
        public int Flip { get; set; }
        public BaseLightRotationBase[] Events { get; set; }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightRotationEventBox.ToJson(this)
            };

        public override BaseItem Clone() => new BaseLightRotationEventBox(this);

        public override IReadOnlyList<BaseGLSEvent> ReadOnlyEvents => Events;

        public override void ClearEvents() => Events = Array.Empty<BaseLightRotationBase>();
        
        // Rotation-axis mutations use the shared occupied-beat replacement invariant before restoring their typed array.
        public override void SetEvents(BaseGLSEvent[] data) =>
            Events = ResolveSameBeatConflicts(data).OfType<BaseLightRotationBase>().ToArray();

        public override Axis GetAxis() => (Axis)Axis;
    }
}
