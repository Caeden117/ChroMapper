using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationEventBoxGroup : BaseLightTransformEventBoxGroup<BaseLightRotationEventBox>
    {
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSRotation;

        public BaseLightRotationEventBoxGroup()
        {
        }

        protected BaseLightRotationEventBoxGroup(
            float time,
            int id,
            JSONNode customData = null) : base(time, id, customData)
        {
        }

        protected BaseLightRotationEventBoxGroup(BaseLightRotationEventBoxGroup other) : base(
            other.JsonTime,
            other.ID,
            other.CustomData?.Clone())
        {
            CloneTransformBoxesFrom(other);
        }

        public BaseLightRotationEventBoxGroup(JSONNode node) : this(BeatmapFactory.LightRotationEventBoxGroups(node)) { }

        public override bool[] GetEnabledAxes(TrackDefinitionGLS trackDefinition) => trackDefinition.RotationTracks;

        protected override BaseLightRotationEventBox CreateTransformBoxCore(int axis) => new() { Axis = axis };

        public override string CustomKeyColor { get; } = "unusedKeyColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightRotationEventBoxGroup.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightRotationEventBoxGroup(this);
    }
}
