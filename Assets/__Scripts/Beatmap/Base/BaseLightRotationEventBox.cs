using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationEventBox : BaseLightTransformEventBox
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
            beatDistributionType,
            axis,
            flip)
        {
            RotationDistribution = rotationDistribution;
            RotationDistributionType = rotationDistributionType;
            RotationAffectFirst = rotationAffectFirst;
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
            easing,
            axis,
            flip)
        {
            RotationDistribution = rotationDistribution;
            RotationDistributionType = rotationDistributionType;
            RotationAffectFirst = rotationAffectFirst;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightRotationEventBox(BaseLightRotationEventBox other) : base(other)
        {
            RotationDistribution = other.RotationDistribution;
            RotationDistributionType = other.RotationDistributionType;
            RotationAffectFirst = other.RotationAffectFirst;
            Events = other.Events.Select(x => x.Clone()).Cast<BaseLightRotationBase>().ToArray();
        }

        public float RotationDistribution { get; set; }
        public int RotationDistributionType { get; set; }
        public int RotationAffectFirst { get; set; }
        public BaseLightRotationBase[] Events { get; set; }

        // Map to shared transform interface
        public override float ValueDistribution
        {
            get => RotationDistribution;
            set => RotationDistribution = value;
        }

        public override int ValueDistributionType
        {
            get => RotationDistributionType;
            set => RotationDistributionType = value;
        }

        public override int AffectFirst
        {
            get => RotationAffectFirst;
            set => RotationAffectFirst = value;
        }

        public override float ValueDistributionDisplayScale => 1f;

        public override bool AcceptsEvent(BaseGLSEvent evt) => evt is BaseLightRotationBase;

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

    }
}
