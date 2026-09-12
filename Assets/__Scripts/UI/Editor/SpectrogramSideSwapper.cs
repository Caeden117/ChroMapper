using UnityEngine;
using UnityEngine.Serialization;

public class SpectrogramSideSwapper : MonoBehaviour
{
    [SerializeField] private GridLane spectrogramGridLane;
    // Expose the non-editable visualization lane so placement systems can exclude it from valid editor targets.
    public static GridLane SpectrogramGridLane { get; private set; }
    private bool IsNoteSide { get; set; } = true;

    private void Awake() => SpectrogramGridLane = spectrogramGridLane;

    private void OnDestroy()
    {
        if (SpectrogramGridLane == spectrogramGridLane)
        {
            SpectrogramGridLane = null;
        }
    }

    public void SwapSides()
    {
        IsNoteSide = !IsNoteSide;
        var order = IsNoteSide ? -1000 : 1000;
        spectrogramGridLane.Order = order;
    }
}
