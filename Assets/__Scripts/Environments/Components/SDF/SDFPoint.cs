using UnityEngine;

public class SDFPoint : MonoBehaviour
{
    [Tooltip("Use negative radius for the SDF to act as a negative force (multiplying rather than subtracting)")]
    [SerializeField]
    public float Radius = 1f;

    public float SqrtRadius { get; set; } = 1f;

    protected void Awake() => SqrtRadius = Radius;
}
