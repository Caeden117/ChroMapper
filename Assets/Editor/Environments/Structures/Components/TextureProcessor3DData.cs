using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class TextureProcessor3DData : EnvironmentComponentData<TextureProcessor3D>
{
    public string TextureGenCompute;
    public string WriteTexturesCompute;
    public string[] InputTextures;

    public int RowSize;
    public int ColumnSize;
    public int DepthSize;

    public MotionPreset[] PresetArray;

    public int ActivePresetIndex;

    public string[] MaterialsUsingOutput;

    public override void FillComponents(GameObject self, TextureProcessor3D comp, CreateContainer container)
    {
        comp.TextureGenCompute = container.Library.ComputeShaders.Find(x => x.name == TextureGenCompute).computeShader;
        comp.WriteTexturesCompute =
            container.Library.ComputeShaders.Find(x => x.name == WriteTexturesCompute).computeShader;
        comp.InputTextures = InputTextures.Select(x => container.Library.Textures.Lookup[x] as Texture2D).ToArray();
        comp.MaterialsUsingOutput = MaterialsUsingOutput
            .Select(container.GetMaterialSafe)
            .ToArray();
        comp.PresetArray = PresetArray.Select(x => x.Create()).ToArray();

        comp.RowSize = RowSize;
        comp.ColumnSize = ColumnSize;
        comp.DepthSize = DepthSize;
        comp.ActivePresetIndex = ActivePresetIndex;
        comp.Step();
    }

    public class ChannelParams
    {
        [JsonProperty("_computeKernel")] public int ComputeKernel;
        [JsonProperty("_inputTextureIndex")] public int InputTextureIndex;
        [JsonProperty("_speed")] public float Speed;
        [JsonProperty("_spatialScale")] public float SpatialScale;
        [JsonProperty("_phase")] public float Phase;
        [JsonProperty("_param1")] public float Param1;
        [JsonProperty("_param2")] public float Param2;
        [JsonProperty("_outputOffset")] public float OutputOffset;

        public TextureProcessor3D.ChannelParams Create()
        {
            return new TextureProcessor3D.ChannelParams
            {
                ComputeKernel = (TextureProcessor3D.ComputeKernel)ComputeKernel,
                InputTextureIndex = InputTextureIndex,
                Speed = Speed,
                SpatialScale = SpatialScale,
                Phase = Phase,
                Param1 = Param1,
                Param2 = Param2,
                OutputOffset = OutputOffset
            };
        }
    }

    public class MotionPreset
    {
        public ChannelParams ChannelA;
        public ChannelParams ChannelB;
        public ChannelParams ChannelC;
        public ChannelParams ChannelD;

        public TextureProcessor3D.MotionPreset Create()
        {
            return new TextureProcessor3D.MotionPreset
            {
                ChannelA = ChannelA.Create(),
                ChannelB = ChannelB.Create(),
                ChannelC = ChannelC.Create(),
                ChannelD = ChannelD.Create()
            };
        }
    }
}
