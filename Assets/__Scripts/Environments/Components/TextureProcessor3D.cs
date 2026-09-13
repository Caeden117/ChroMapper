using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class TextureProcessor3D : MonoBehaviour
{
    [Serializable]
    public struct ChannelParams
    {
        public ComputeKernel ComputeKernel;
        public int InputTextureIndex;
        public float Speed;
        public float SpatialScale;
        public float Phase;
        public float Param1;
        public float Param2;
        public float OutputOffset;
    }

    [Serializable]
    public struct MotionPreset
    {
        public ChannelParams ChannelA;
        public ChannelParams ChannelB;
        public ChannelParams ChannelC;
        public ChannelParams ChannelD;
    }

    private struct ChannelParamsChangeIntention
    {
        public ComputeKernel? ComputeKernel;
        public int? InputTextureIndex;
        public float? Speed;
        public float? SpatialScale;
        public float? Phase;
        public float? Param1;
        public float? Param2;
        public float? OutputOffset;
    }

    private struct MotionPresetChangeIntention
    {
        public ChannelParamsChangeIntention ChannelA;
        public ChannelParamsChangeIntention ChannelB;
        public ChannelParamsChangeIntention ChannelC;
        public ChannelParamsChangeIntention ChannelD;
    }

    public enum ComputeKernel
    {
        Constant,
        Texture,
        PlanarWave,
        CylindricalWave,
        SphericalWave,
        PerlinNoise3D,
        Ramp3D,
        SplitPlane,
        WaveRipple,
        RectRipple,
        TextureMaskMult,
        TextureMaskAdd
    }

    [SerializeField] public ComputeShader TextureGenCompute;
    [SerializeField] public ComputeShader WriteTexturesCompute;
    [SerializeField] public Texture2D[] InputTextures;
    [SerializeField] public Material[] MaterialsUsingOutput = Array.Empty<Material>();

    [SerializeField] public int RowSize;
    [SerializeField] public int ColumnSize;
    [SerializeField] public int DepthSize;

    [SerializeField] public MotionPreset[] PresetArray = new MotionPreset[10];
    [SerializeField] public int ActivePresetIndex;
    private int activePresetIndexOld;

    private Matrix4x4[] matrices;
    private int numInstances;

    private RenderTexture animationTextureA;
    private RenderTexture animationTextureB;
    private RenderTexture animationTextureC;
    private RenderTexture animationTextureD;
    private RenderTexture animationTextureOut;

    private int textureArrayLength;
    private int testMaterialArrayCount;
    private int kernelIndexMax;

    private MotionPresetChangeIntention activePresetChangeIntention;

    private static readonly string[] kernelStrings =
    {
        "Constant",
        "Texture",
        "PlanarWave",
        "CylindricalWave",
        "SphericalWave",
        "PerlinNoise3D",
        "Ramp3D",
        "SplitPlane",
        "WaveRipple",
        "RectRipple",
        "TextureMaskMult",
        "TextureMaskAdd"
    };

    private static readonly int speedId = Shader.PropertyToID("_speed");
    private static readonly int spatialScaleId = Shader.PropertyToID("_spatialScale");
    private static readonly int columnSizeId = Shader.PropertyToID("_columnSize");
    private static readonly int rowSizeId = Shader.PropertyToID("_rowSize");
    private static readonly int depthSizeId = Shader.PropertyToID("_depthSize");
    private static readonly int phaseId = Shader.PropertyToID("_phase");
    private static readonly int param1Id = Shader.PropertyToID("_param1");
    private static readonly int param2Id = Shader.PropertyToID("_param2");
    private static readonly int outputOffsetId = Shader.PropertyToID("_outputOffset");
    private static readonly int inputTextureId = Shader.PropertyToID("_inputTexture");
    private static readonly int outputTextureId = Shader.PropertyToID("_outputTexture");
    private static readonly int inputTextureAId = Shader.PropertyToID("_inputTextureA");
    private static readonly int inputTextureBId = Shader.PropertyToID("_inputTextureB");
    private static readonly int inputTextureCId = Shader.PropertyToID("_inputTextureC");
    private static readonly int inputTextureDId = Shader.PropertyToID("_inputTextureD");
    private static readonly int outputMaskId = Shader.PropertyToID("_outputMask");
    private static readonly int lookupTexId = Shader.PropertyToID("_LookupTex");

    public ComputeKernel ComputeKernelA
    {
        get => activePresetChangeIntention.ChannelA.ComputeKernel ?? ActivePreset.ChannelA.ComputeKernel;
        set =>
            activePresetChangeIntention.ChannelA.ComputeKernel =
                (ComputeKernel)Math.Clamp((int)value, 0, kernelIndexMax);
    }

    public ComputeKernel ComputeKernelB
    {
        get => activePresetChangeIntention.ChannelB.ComputeKernel ?? ActivePreset.ChannelB.ComputeKernel;
        set =>
            activePresetChangeIntention.ChannelB.ComputeKernel =
                (ComputeKernel)Math.Clamp((int)value, 0, kernelIndexMax);
    }

    public ComputeKernel ComputeKernelC
    {
        get => activePresetChangeIntention.ChannelC.ComputeKernel ?? ActivePreset.ChannelC.ComputeKernel;
        set =>
            activePresetChangeIntention.ChannelC.ComputeKernel =
                (ComputeKernel)Math.Clamp((int)value, 0, kernelIndexMax);
    }

    public ComputeKernel ComputeKernelD
    {
        get => activePresetChangeIntention.ChannelD.ComputeKernel ?? ActivePreset.ChannelD.ComputeKernel;
        set =>
            activePresetChangeIntention.ChannelD.ComputeKernel =
                (ComputeKernel)Math.Clamp((int)value, 0, kernelIndexMax);
    }

    public int InputTextureIndexA
    {
        get =>
            activePresetChangeIntention.ChannelA.InputTextureIndex
            ?? ActivePreset.ChannelA.InputTextureIndex;
        set => activePresetChangeIntention.ChannelA.InputTextureIndex = value;
    }

    public int InputTextureIndexB
    {
        get =>
            activePresetChangeIntention.ChannelB.InputTextureIndex
            ?? ActivePreset.ChannelB.InputTextureIndex;
        set => activePresetChangeIntention.ChannelB.InputTextureIndex = value;
    }

    public int InputTextureIndexC
    {
        get =>
            activePresetChangeIntention.ChannelC.InputTextureIndex
            ?? ActivePreset.ChannelC.InputTextureIndex;
        set => activePresetChangeIntention.ChannelC.InputTextureIndex = value;
    }

    public int InputTextureIndexD
    {
        get =>
            activePresetChangeIntention.ChannelD.InputTextureIndex
            ?? ActivePreset.ChannelD.InputTextureIndex;
        set => activePresetChangeIntention.ChannelD.InputTextureIndex = value;
    }

    public float SpeedA
    {
        get => activePresetChangeIntention.ChannelA.Speed ?? ActivePreset.ChannelA.Speed;
        set => activePresetChangeIntention.ChannelA.Speed = value;
    }

    public float SpeedB
    {
        get => activePresetChangeIntention.ChannelB.Speed ?? ActivePreset.ChannelB.Speed;
        set => activePresetChangeIntention.ChannelB.Speed = value;
    }

    public float SpeedC
    {
        get => activePresetChangeIntention.ChannelC.Speed ?? ActivePreset.ChannelC.Speed;
        set => activePresetChangeIntention.ChannelC.Speed = value;
    }

    public float SpeedD
    {
        get => activePresetChangeIntention.ChannelD.Speed ?? ActivePreset.ChannelD.Speed;
        set => activePresetChangeIntention.ChannelD.Speed = value;
    }

    public float SpatialScaleA
    {
        get => activePresetChangeIntention.ChannelA.SpatialScale ?? ActivePreset.ChannelA.SpatialScale;
        set => activePresetChangeIntention.ChannelA.SpatialScale = value;
    }

    public float SpatialScaleB
    {
        get => activePresetChangeIntention.ChannelB.SpatialScale ?? ActivePreset.ChannelB.SpatialScale;
        set => activePresetChangeIntention.ChannelB.SpatialScale = value;
    }

    public float SpatialScaleC
    {
        get => activePresetChangeIntention.ChannelC.SpatialScale ?? ActivePreset.ChannelC.SpatialScale;
        set => activePresetChangeIntention.ChannelC.SpatialScale = value;
    }

    public float SpatialScaleD
    {
        get => activePresetChangeIntention.ChannelB.SpatialScale ?? ActivePreset.ChannelB.SpatialScale;
        set => activePresetChangeIntention.ChannelD.SpatialScale = value;
    }

    public float PhaseA
    {
        get => activePresetChangeIntention.ChannelA.Phase ?? ActivePreset.ChannelA.Phase;
        set => activePresetChangeIntention.ChannelA.Phase = value;
    }

    public float PhaseB
    {
        get => activePresetChangeIntention.ChannelB.Phase ?? ActivePreset.ChannelB.Phase;
        set => activePresetChangeIntention.ChannelB.Phase = value;
    }

    public float PhaseC
    {
        get => activePresetChangeIntention.ChannelC.Phase ?? ActivePreset.ChannelC.Phase;
        set => activePresetChangeIntention.ChannelC.Phase = value;
    }

    public float PhaseD
    {
        get => activePresetChangeIntention.ChannelD.Phase ?? ActivePreset.ChannelD.Phase;
        set => activePresetChangeIntention.ChannelD.Phase = value;
    }

    public float Param1A
    {
        get => activePresetChangeIntention.ChannelA.Param1 ?? ActivePreset.ChannelA.Param1;
        set => activePresetChangeIntention.ChannelA.Param1 = value;
    }

    public float Param1B
    {
        get => activePresetChangeIntention.ChannelB.Param1 ?? ActivePreset.ChannelB.Param1;
        set => activePresetChangeIntention.ChannelB.Param1 = value;
    }

    public float Param1C
    {
        get => activePresetChangeIntention.ChannelC.Param1 ?? ActivePreset.ChannelC.Param1;
        set => activePresetChangeIntention.ChannelC.Param1 = value;
    }

    public float Param1D
    {
        get => activePresetChangeIntention.ChannelD.Param1 ?? ActivePreset.ChannelD.Param1;
        set => activePresetChangeIntention.ChannelD.Param1 = value;
    }

    public float Param2A
    {
        get => activePresetChangeIntention.ChannelA.Param2 ?? ActivePreset.ChannelA.Param2;
        set => activePresetChangeIntention.ChannelA.Param2 = value;
    }

    public float Param2B
    {
        get => activePresetChangeIntention.ChannelB.Param2 ?? ActivePreset.ChannelB.Param2;
        set => activePresetChangeIntention.ChannelB.Param2 = value;
    }

    public float Param2C
    {
        get => activePresetChangeIntention.ChannelC.Param2 ?? ActivePreset.ChannelC.Param2;
        set => activePresetChangeIntention.ChannelC.Param2 = value;
    }

    public float Param2D
    {
        get => activePresetChangeIntention.ChannelD.Param2 ?? ActivePreset.ChannelD.Param2;
        set => activePresetChangeIntention.ChannelD.Param2 = value;
    }

    public float OutputOffsetA
    {
        get => activePresetChangeIntention.ChannelA.OutputOffset ?? ActivePreset.ChannelA.OutputOffset;
        set => activePresetChangeIntention.ChannelA.OutputOffset = value;
    }

    public float OutputOffsetB
    {
        get => activePresetChangeIntention.ChannelB.OutputOffset ?? ActivePreset.ChannelB.OutputOffset;
        set => activePresetChangeIntention.ChannelB.OutputOffset = value;
    }

    public float OutputOffsetC
    {
        get => activePresetChangeIntention.ChannelC.OutputOffset ?? ActivePreset.ChannelC.OutputOffset;
        set => activePresetChangeIntention.ChannelC.OutputOffset = value;
    }

    public float OutputOffsetD
    {
        get => activePresetChangeIntention.ChannelD.OutputOffset ?? ActivePreset.ChannelD.OutputOffset;
        set => activePresetChangeIntention.ChannelD.OutputOffset = value;
    }

    private ref MotionPreset ActivePreset => ref PresetArray[ActivePresetIndex];

    protected void Awake()
    {
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);
        numInstances = ColumnSize * RowSize * DepthSize;
        kernelIndexMax = kernelStrings.Length - 1;
        activePresetIndexOld = ActivePresetIndex;
        UpdateBuffers();
    }

    protected void OnEnable() => UpdateBuffers();
    protected void OnValidate() => UpdateBuffers();

    protected void LateUpdate()
    {
        if (ActivePresetIndex != activePresetIndexOld)
        {
            UpdateBuffers();
            activePresetIndexOld = ActivePresetIndex;
        }

        numInstances = ColumnSize * RowSize * DepthSize;
        AnimateTextures();
    }

    protected void OnDisable() => ReleaseTextures();
    protected void OnDestroy() => ReleaseTextures();

    private static RenderTexture CreateTexture(int sizeX, int sizeY, int sizeZ)
    {
        var renderTexture = new RenderTexture(sizeX, sizeY, 0)
        {
            dimension = TextureDimension.Tex3D,
            volumeDepth = sizeZ,
            format = RenderTextureFormat.ARGBFloat,
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();
        return renderTexture;
    }

    private void ReleaseTextures()
    {
        if ((bool)animationTextureA)
        {
            animationTextureA.Release();
            animationTextureA = null;
        }

        if ((bool)animationTextureB)
        {
            animationTextureB.Release();
            animationTextureB = null;
        }

        if ((bool)animationTextureC)
        {
            animationTextureC.Release();
            animationTextureC = null;
        }

        if ((bool)animationTextureD)
        {
            animationTextureD.Release();
            animationTextureD = null;
        }

        if ((bool)animationTextureOut)
        {
            animationTextureOut.Release();
            animationTextureOut = null;
        }
    }

    private void UpdateBuffers()
    {
        if (RowSize <= 0 || ColumnSize <= 0 || DepthSize <= 0) return;
        numInstances = ColumnSize * RowSize * DepthSize;
        ReleaseTextures();
        animationTextureA = CreateTexture(RowSize, ColumnSize, DepthSize);
        animationTextureB = CreateTexture(RowSize, ColumnSize, DepthSize);
        animationTextureC = CreateTexture(RowSize, ColumnSize, DepthSize);
        animationTextureD = CreateTexture(RowSize, ColumnSize, DepthSize);
        animationTextureOut = CreateTexture(RowSize, ColumnSize, DepthSize);
        foreach (var material in MaterialsUsingOutput)
            if (material != null && material.HasProperty(lookupTexId))
                material.SetTexture(lookupTexId, animationTextureOut);
    }

    private void AnimateTextures()
    {
        textureArrayLength = InputTextures.Length;
        if (RowSize <= 0 || ColumnSize <= 0 || DepthSize <= 0 || textureArrayLength == 0) return;
        ApplyIntention();
        AnimateChannel(ref ActivePreset.ChannelA, animationTextureA);
        AnimateChannel(ref ActivePreset.ChannelB, animationTextureB);
        AnimateChannel(ref ActivePreset.ChannelC, animationTextureC);
        AnimateChannel(ref ActivePreset.ChannelD, animationTextureD);
        var kernelIndex = WriteTexturesCompute.FindKernel("WriteTextures");
        WriteTexturesCompute.SetInt(columnSizeId, ColumnSize);
        WriteTexturesCompute.SetInt(rowSizeId, RowSize);
        WriteTexturesCompute.SetInt(depthSizeId, DepthSize);
        WriteTexturesCompute.SetTexture(kernelIndex, inputTextureAId, animationTextureA);
        WriteTexturesCompute.SetTexture(kernelIndex, inputTextureBId, animationTextureB);
        WriteTexturesCompute.SetTexture(kernelIndex, inputTextureCId, animationTextureC);
        WriteTexturesCompute.SetTexture(kernelIndex, inputTextureDId, animationTextureD);
        WriteTexturesCompute.SetTexture(kernelIndex, outputMaskId, animationTextureOut);
        WriteTexturesCompute.Dispatch(kernelIndex, RowSize, ColumnSize, DepthSize);
    }

    private void AnimateChannel(ref ChannelParams channel, RenderTexture outputTexture)
    {
        var kernelName = GetKernelName(channel.ComputeKernel);
        var kernelIndex = TextureGenCompute.FindKernel(kernelName);
        TextureGenCompute.SetFloat(speedId, channel.Speed);
        TextureGenCompute.SetFloat(spatialScaleId, channel.SpatialScale);
        TextureGenCompute.SetInt(columnSizeId, ColumnSize);
        TextureGenCompute.SetInt(rowSizeId, RowSize);
        TextureGenCompute.SetInt(depthSizeId, DepthSize);
        TextureGenCompute.SetFloat(phaseId, channel.Phase);
        TextureGenCompute.SetFloat(param1Id, channel.Param1);
        TextureGenCompute.SetFloat(param2Id, channel.Param2);
        TextureGenCompute.SetFloat(outputOffsetId, channel.OutputOffset);
        TextureGenCompute.SetTexture(
            kernelIndex,
            inputTextureId,
            InputTextures[Math.Abs(channel.InputTextureIndex) % textureArrayLength]);
        TextureGenCompute.SetTexture(kernelIndex, outputTextureId, outputTexture);
        TextureGenCompute.Dispatch(kernelIndex, RowSize, ColumnSize, DepthSize);
    }

    private void ApplyIntention()
    {
        ApplyChannelIntention(ref ActivePreset.ChannelA, ref activePresetChangeIntention.ChannelA);
        ApplyChannelIntention(ref ActivePreset.ChannelB, ref activePresetChangeIntention.ChannelB);
        ApplyChannelIntention(ref ActivePreset.ChannelC, ref activePresetChangeIntention.ChannelC);
        ApplyChannelIntention(ref ActivePreset.ChannelD, ref activePresetChangeIntention.ChannelD);
        return;

        static void ApplyChannelIntention(ref ChannelParams channel, ref ChannelParamsChangeIntention channelIntention)
        {
            ApplyParamIntention(ref channel.ComputeKernel, ref channelIntention.ComputeKernel);
            ApplyParamIntention(ref channel.InputTextureIndex, ref channelIntention.InputTextureIndex);
            ApplyParamIntention(ref channel.Speed, ref channelIntention.Speed);
            ApplyParamIntention(ref channel.SpatialScale, ref channelIntention.SpatialScale);
            ApplyParamIntention(ref channel.Phase, ref channelIntention.Phase);
            ApplyParamIntention(ref channel.Param1, ref channelIntention.Param1);
            ApplyParamIntention(ref channel.Param2, ref channelIntention.Param2);
            ApplyParamIntention(ref channel.OutputOffset, ref channelIntention.OutputOffset);
        }

        static void ApplyParamIntention<T>(ref T param, ref T? intention) where T : struct
        {
            if (!intention.HasValue) return;
            param = intention.Value;
            intention = null;
        }
    }

    public void ModifyGridSize(int rowSizeDelta, int columnSizeDelta, int depthSizeDelta)
    {
        RowSize = Math.Max(RowSize + rowSizeDelta, 1);
        ColumnSize = Math.Max(ColumnSize + columnSizeDelta, 1);
        DepthSize = Math.Max(DepthSize + depthSizeDelta, 1);
    }

    public void Step() => UpdateBuffers();

    private string GetKernelName(ComputeKernel kernel) => kernelStrings[Math.Clamp((int)kernel, 0, kernelIndexMax)];
}
