using UnityEngine;

public class GridElementController : MonoBehaviour
{
    [SerializeField] public Transform GridPivotAnchor;
    [SerializeField] public MaterialPropertyBlockController MaterialPropertyBlockController;
    [SerializeField] public MeshRenderer GridElementRenderer;
    [SerializeField] public string GridPivotPropertyName;
    [SerializeField] public string GridElementIndexPropertyName;
    [SerializeField] public Vector3 IDVector = new(0f, 0f, 0f);

    private int gridPivotId;
    private int gridElementIndexId;

    private bool hasInitialized;

    protected void Awake() => Initialize();

    private void Initialize()
    {
        if (hasInitialized || GridPivotAnchor == null || MaterialPropertyBlockController == null) return;
        hasInitialized = true;

        gridPivotId = Shader.PropertyToID(GridPivotPropertyName);
        gridElementIndexId = Shader.PropertyToID(GridElementIndexPropertyName);

        var objectSpacePivot = ConvertPositionToObjectSpace(GridPivotAnchor.position);

        MaterialPropertyBlockController.Mpb.SetVector(gridPivotId, objectSpacePivot);
        MaterialPropertyBlockController.Mpb.SetVector(gridElementIndexId, IDVector);
        MaterialPropertyBlockController.ApplyChanges();
    }

    private Vector3 ConvertPositionToObjectSpace(Vector3 worldSpacePivotPosition) =>
        transform.InverseTransformPoint(worldSpacePivotPosition);

    public void SetGridMaterial(Material material)
    {
        hasInitialized = false;
        GridElementRenderer.sharedMaterial = material;
        Initialize();
    }
}
