using UnityEngine;

[CreateAssetMenu(fileName = "ReflectionProbeData", menuName = "Environment/Reflection Probe Data")]
public class ReflectionProbeDataSO : ScriptableObject
{
    public Cubemap ReflectionProbeCubemap1;
    public Cubemap ReflectionProbeCubemap2;
}
