using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public class V2Material
    {
        public static string KeyColor = "_color";
        public static string KeyShader = "_shader";
        public static string KeyTrack = "_track";
        public static string KeyShaderKeywords = "_shaderKeywords";
        
        public static BaseMaterial GetFromJson(JSONNode node) => new BaseMaterial(node);

        public static JSONNode ToJson(BaseMaterial material)
        {
            var node = new JSONObject();
            if (material.Color != null) node[KeyColor] = material.Color;
            node[KeyShader] = material.Shader;
            if (material.Track != null) node[KeyTrack] = material.Track;
            if (material.ShaderKeywords.Count > 0)
            {
                var keywords = new JSONArray();
                foreach (var keyword in material.ShaderKeywords)
                {
                    keywords.Add(keyword);
                }
                node[KeyShaderKeywords] = keywords;
            }
            return node;
        }
    }
}
