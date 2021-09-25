using UnityEngine;

public class SpectrogramSideSwapper : MonoBehaviour
{
    [SerializeField] private GridChild spectrogramGridChild;
    [SerializeField] private GridChild spectrogramChunksChild;
    public bool IsNoteSide { get; set; } = true;

    public void SwapSides()
    {
        IsNoteSide = !IsNoteSide;

        var order = IsNoteSide ? -1 : 3;
        var offset = IsNoteSide ? 3.5f : 2.5f;

        GridOrderController.DeregisterChild(spectrogramChunksChild);
        GridOrderController.DeregisterChild(spectrogramGridChild);

        spectrogramChunksChild.Order = spectrogramGridChild.Order = order;
        spectrogramGridChild.LocalOffset = new Vector3(offset, 0, 0);
        spectrogramChunksChild.LocalOffset = new Vector3(offset - 2, 0, 0);

        GridOrderController.RegisterChild(spectrogramChunksChild);
        GridOrderController.RegisterChild(spectrogramGridChild);
    }
}
