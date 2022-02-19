using UnityEngine;
using UnityEngine.Serialization;

public class MaterialParameters : MonoBehaviour
{
    [FormerlySerializedAs("flashShaderOffset")] public Vector3 FlashShaderOffset = Vector3.zero;
    [FormerlySerializedAs("fadeShaderOffset")] public Vector3 FadeShaderOffset = Vector3.zero;
    [FormerlySerializedAs("defaultFadeSize")] public float DefaultFadeSize = 0f;
    [FormerlySerializedAs("boostEventFadeSize")] public float BoostEventFadeSize = 0f;
}
