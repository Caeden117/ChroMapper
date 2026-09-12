using UnityEngine;

[ExecuteAlways]
public class GridPlane : MonoBehaviour
{
    [SerializeField] public Renderer Grid;
    [SerializeField] public Renderer Interface;
    // Grid hit surfaces are a core plane dependency, so callers should not rediscover the colocated collider at runtime.
    [SerializeField] public IntersectionCollider IntersectionCollider;

    [Header("Visual")] [SerializeField] private Color gridColor = Color.white;
    [SerializeField] private Color interfaceColor = Color.gray;

    [SerializeField] private Material opaqueInterface;
    [SerializeField] private Material transparentInterface;

    [SerializeField] private Vector4 spacing = new(1f, 1f / 4f, 1f / 8f, 1f / 16f);
    [SerializeField] private Vector4 thickness = new(0.1f, 0.05f, 0.025f, 0.0125f);
    [SerializeField] private Vector3 offset = Vector3.zero;
    [SerializeField] private float scale = 1f;

    private MaterialPropertyBlock gridMaterialPropertyBlock;
    private MaterialPropertyBlock interfaceMaterialPropertyBlock;

    private static readonly int colorID = Shader.PropertyToID("_Color");
    private static readonly int gridSpacingID = Shader.PropertyToID("_GridSpacing");
    private static readonly int gridThicknessID = Shader.PropertyToID("_GridThickness");
    private static readonly int gridOffsetID = Shader.PropertyToID("_GridOffset");
    private static readonly int gridScaleID = Shader.PropertyToID("_GridScale");

    public void OnValidate() => RefreshVisual();

    public void Start() => RefreshVisual();

    public void SetGridColor(Color color) => gridColor = color;
    public void SetInterfaceColor(Color color) => interfaceColor = color;
    public void SetSpacing(Vector4 vector) => spacing = vector;
    public void SetThickness(Vector4 vector) => thickness = vector;
    public void SetOffset(Vector3 vector) => offset = vector;
    public void SetScale(float value) => scale = value;

    public void RefreshVisual()
    {
        gridMaterialPropertyBlock ??= new MaterialPropertyBlock();
        interfaceMaterialPropertyBlock ??= new MaterialPropertyBlock();

        Interface.sharedMaterial = Mathf.Approximately(interfaceColor.a, 1f) ? opaqueInterface : transparentInterface;

        interfaceMaterialPropertyBlock.SetColor(colorID, interfaceColor);
        gridMaterialPropertyBlock.SetColor(colorID, gridColor);
        gridMaterialPropertyBlock.SetVector(gridSpacingID, spacing * scale);
        gridMaterialPropertyBlock.SetVector(gridThicknessID, thickness * scale);
        gridMaterialPropertyBlock.SetVector(gridOffsetID, offset);
        gridMaterialPropertyBlock.SetFloat(gridScaleID, scale);

        Grid.SetPropertyBlock(gridMaterialPropertyBlock);
        Interface.SetPropertyBlock(interfaceMaterialPropertyBlock);
    }
}
