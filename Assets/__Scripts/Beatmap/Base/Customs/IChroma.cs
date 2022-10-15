using Beatmap.Shared;
using UnityEngine;

namespace Beatmap.Base.Customs
{
    public interface IChromaObject
    {
        Color? CustomColor { get; set; }

        string CustomKeyColor { get; }
    }

    public interface IChromaEvent : IChromaObject
    {
        int CustomPropID { get; set; } // honestly, what is this? kinda misleading
        int[] CustomLightID { get; set; }
        string CustomLerpType { get; set; }
        string CustomEasing { get; set; }
        ChromaLightGradient CustomLightGradient { get; set; }

        float? CustomStep { get; set; }
        float? CustomProp { get; set; }
        float? CustomSpeed { get; set; }
        float? CustomStepMult { get; set; }
        float? CustomPropMult { get; set; }
        float? CustomSpeedMult { get; set; }
        float? CustomPreciseSpeed { get; set; }
        int? CustomDirection { get; set; }
        bool? CustomLockRotation { get; set; }
        string CustomNameFilter { get; set; }

        string CustomKeyPropID { get; }
        string CustomKeyLightID { get; }
        string CustomKeyLerpType { get; }
        string CustomKeyEasing { get; }
        string CustomKeyLightGradient { get; }

        string CustomKeyStep { get; }
        string CustomKeyProp { get; }
        string CustomKeySpeed { get; }
        string CustomKeyStepMult { get; }
        string CustomKeyPropMult { get; }
        string CustomKeySpeedMult { get; }
        string CustomKeyPreciseSpeed { get; }
        string CustomKeyDirection { get; }
        string CustomKeyLockRotation { get; }
        string CustomKeyNameFilter { get; }

        bool IsLegacyChroma { get; }
        bool IsPropagation { get; }
    }
}
