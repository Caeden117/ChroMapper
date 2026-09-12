using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightTranslationEventBoxGroup : BaseLightTransformEventBoxGroup<BaseLightTranslationEventBox>
    {
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSTranslation;

        public BaseLightTranslationEventBoxGroup()
        {
        }

        protected BaseLightTranslationEventBoxGroup(
            float time,
            int id,
            JSONNode customData = null) : base(time, id, customData)
        {
        }

        protected BaseLightTranslationEventBoxGroup(BaseLightTranslationEventBoxGroup other) : base(
            other.JsonTime,
            other.ID,
            other.CustomData?.Clone())
        {
            CloneTransformBoxesFrom(other);
        }

        public BaseLightTranslationEventBoxGroup(JSONNode node) : this(BeatmapFactory.LightTranslationEventBoxGroups(node)) { }

        public override bool[] GetEnabledAxes(TrackDefinitionGLS trackDefinition) => trackDefinition.TranslationTracks;

        protected override BaseLightTranslationEventBox CreateTransformBoxCore(int axis) => new() { Axis = axis };

        public override string CustomKeyColor { get; } = "unusedKeyColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightTranslationEventBoxGroup.ToJson(this)
            };

        public override BaseItem Clone() => new BaseLightTranslationEventBoxGroup(this);
    }
}
