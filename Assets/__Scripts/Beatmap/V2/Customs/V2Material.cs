using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public class V2Material : BaseMaterial, V2Object
    {
        public V2Material(BaseMaterial other) : base(other) { }

        public V2Material(JSONNode node) : base(node) { }

        public override string KeyColor { get; } = "_color";
        public override string KeyShader { get; } = "_shader";
        public override string KeyTrack { get; } = "_track";
        public override string KeyShaderKeywords { get; } = "_shaderKeywords";

        public override BaseItem Clone() => new V2Material(this);
    }
}
