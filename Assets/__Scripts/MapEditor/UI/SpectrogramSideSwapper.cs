using UnityEngine;

public class SpectrogramSideSwapper : MonoBehaviour
{
    public bool IsNoteSide { get; set; } = true;

    [SerializeField] private GridChild spectrogramGridChild;
    [SerializeField] private GridChild spectrogramChunksChild;

    public void SwapSides()
    {
        IsNoteSide = !IsNoteSide;

        int order = IsNoteSide ? -1 : 3;
        float offset = IsNoteSide ? 3.5f : 2.5f;

        GridOrderController.DeregisterChild(spectrogramChunksChild);
        GridOrderController.DeregisterChild(spectrogramGridChild);

        spectrogramChunksChild.Order = spectrogramGridChild.Order = order;
        spectrogramGridChild.LocalOffset = new Vector3(offset, 0, 0);
        spectrogramChunksChild.LocalOffset = new Vector3(offset - 2, 0, 0);

        GridOrderController.RegisterChild(spectrogramChunksChild);
        GridOrderController.RegisterChild(spectrogramGridChild);
    }
}
