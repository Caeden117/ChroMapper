using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public class V3Material : BaseMaterial, V3Object
    {
        public V3Material(BaseMaterial other) : base(other) { }

        public V3Material(JSONNode node) : base(node) { }

        public override string KeyColor { get; } = "color";
        public override string KeyShader { get; } = "shader";
        public override string KeyTrack { get; } = "track";
        public override string KeyShaderKeywords { get; } = "shaderKeywords";

        public override BaseItem Clone() => new V3Material(this);
    }
}
