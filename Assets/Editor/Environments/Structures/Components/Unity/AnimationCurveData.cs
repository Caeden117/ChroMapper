using System.Linq;
using UnityEngine;

public class AnimationCurveData
{
    public CurveKey[] Keys;

    public class CurveKey
    {
        public float Time;
        public float Value;
        public float InTangent;
        public float OutTangent;
        public float InWeight;
        public float OutWeight;
        public WeightedMode WeightedMode;
    }

    public AnimationCurve Create()
    {
        return new AnimationCurve
        {
            keys = Keys
                .Select(x => new Keyframe
                {
                    time = x.Time,
                    value = x.Value,
                    inTangent = x.InTangent,
                    outTangent = x.OutTangent,
                    inWeight = x.InWeight,
                    outWeight = x.OutWeight,
                    weightedMode = x.WeightedMode
                })
                .ToArray(),
            postWrapMode = WrapMode.ClampForever,
            preWrapMode = WrapMode.ClampForever
        };
    }
}
