using UnityEngine;

public class TextureProcessor3DParameterFx : FxTarget
{
    public enum TextureProcessor3DParameter
    {
        ComputeKernel,
        TextureIndex,
        Speed,
        SpatialScale,
        Phase,
        Param1,
        Param2,
        OutputOffset
    }

    public enum TextureProcessor3DChannel
    {
        A,
        B,
        C,
        D
    }

    [SerializeField] public TextureProcessor3D TextureProcessor3D;

    [SerializeField] public TextureProcessor3DParameter Parameter;
    [SerializeField] public TextureProcessor3DChannel Channel;

    [SerializeField] public Vector2 ValueBounds = new(0f, 1f);

    public override void SetValue(int groupId, int elementId, float value) => SetFloat(value);
    public override void TriggerValue(int groupId, int elementId, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        var num = Mathf.Lerp(ValueBounds.x, ValueBounds.y, 0.5f * (value + 1f));
        switch (Channel)
        {
            case TextureProcessor3DChannel.A:
                switch (Parameter)
                {
                    case TextureProcessor3DParameter.ComputeKernel:
                        TextureProcessor3D.ComputeKernelA =
                            (TextureProcessor3D.ComputeKernel)Mathf.RoundToInt(Mathf.Abs(num));
                        break;
                    case TextureProcessor3DParameter.TextureIndex:
                        TextureProcessor3D.InputTextureIndexA = (int)Mathf.Abs(num);
                        break;
                    case TextureProcessor3DParameter.Speed:
                        TextureProcessor3D.SpeedA = num;
                        break;
                    case TextureProcessor3DParameter.SpatialScale:
                        TextureProcessor3D.SpatialScaleA = num;
                        break;
                    case TextureProcessor3DParameter.Phase:
                        TextureProcessor3D.PhaseA = num;
                        break;
                    case TextureProcessor3DParameter.Param1:
                        TextureProcessor3D.Param1A = num;
                        break;
                    case TextureProcessor3DParameter.Param2:
                        TextureProcessor3D.Param2A = num;
                        break;
                    case TextureProcessor3DParameter.OutputOffset:
                        TextureProcessor3D.OutputOffsetA = num;
                        break;
                }

                break;
            case TextureProcessor3DChannel.B:
                switch (Parameter)
                {
                    case TextureProcessor3DParameter.ComputeKernel:
                        TextureProcessor3D.ComputeKernelB =
                            (TextureProcessor3D.ComputeKernel)Mathf.RoundToInt(Mathf.Abs(num));
                        break;
                    case TextureProcessor3DParameter.TextureIndex:
                        TextureProcessor3D.InputTextureIndexB = (int)Mathf.Abs(num);
                        break;
                    case TextureProcessor3DParameter.Speed:
                        TextureProcessor3D.SpeedB = num;
                        break;
                    case TextureProcessor3DParameter.SpatialScale:
                        TextureProcessor3D.SpatialScaleB = num;
                        break;
                    case TextureProcessor3DParameter.Phase:
                        TextureProcessor3D.PhaseB = num;
                        break;
                    case TextureProcessor3DParameter.Param1:
                        TextureProcessor3D.Param1B = num;
                        break;
                    case TextureProcessor3DParameter.Param2:
                        TextureProcessor3D.Param2B = num;
                        break;
                    case TextureProcessor3DParameter.OutputOffset:
                        TextureProcessor3D.OutputOffsetB = num;
                        break;
                }

                break;
            case TextureProcessor3DChannel.C:
                switch (Parameter)
                {
                    case TextureProcessor3DParameter.ComputeKernel:
                        TextureProcessor3D.ComputeKernelC =
                            (TextureProcessor3D.ComputeKernel)Mathf.RoundToInt(Mathf.Abs(num));
                        break;
                    case TextureProcessor3DParameter.TextureIndex:
                        TextureProcessor3D.InputTextureIndexC = (int)Mathf.Abs(num);
                        break;
                    case TextureProcessor3DParameter.Speed:
                        TextureProcessor3D.SpeedC = num;
                        break;
                    case TextureProcessor3DParameter.SpatialScale:
                        TextureProcessor3D.SpatialScaleC = num;
                        break;
                    case TextureProcessor3DParameter.Phase:
                        TextureProcessor3D.PhaseC = num;
                        break;
                    case TextureProcessor3DParameter.Param1:
                        TextureProcessor3D.Param1C = num;
                        break;
                    case TextureProcessor3DParameter.Param2:
                        TextureProcessor3D.Param2C = num;
                        break;
                    case TextureProcessor3DParameter.OutputOffset:
                        TextureProcessor3D.OutputOffsetC = num;
                        break;
                }

                break;
            case TextureProcessor3DChannel.D:
                switch (Parameter)
                {
                    case TextureProcessor3DParameter.ComputeKernel:
                        TextureProcessor3D.ComputeKernelD =
                            (TextureProcessor3D.ComputeKernel)Mathf.RoundToInt(Mathf.Abs(num));
                        break;
                    case TextureProcessor3DParameter.TextureIndex:
                        TextureProcessor3D.InputTextureIndexD = (int)Mathf.Abs(num);
                        break;
                    case TextureProcessor3DParameter.Speed:
                        TextureProcessor3D.SpeedD = num;
                        break;
                    case TextureProcessor3DParameter.SpatialScale:
                        TextureProcessor3D.SpatialScaleD = num;
                        break;
                    case TextureProcessor3DParameter.Phase:
                        TextureProcessor3D.PhaseD = num;
                        break;
                    case TextureProcessor3DParameter.Param1:
                        TextureProcessor3D.Param1D = num;
                        break;
                    case TextureProcessor3DParameter.Param2:
                        TextureProcessor3D.Param2D = num;
                        break;
                    case TextureProcessor3DParameter.OutputOffset:
                        TextureProcessor3D.OutputOffsetD = num;
                        break;
                }

                break;
        }
    }
}
