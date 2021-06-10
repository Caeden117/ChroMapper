using UnityEngine;

public class ApplyIntersectionComputeShader : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;

    private void Awake()
    {
        Intersections.AssignComputeShader(computeShader);
    }

    private void OnDestroy()
    {
        Intersections.DisposeComputeShader();
    }
}
