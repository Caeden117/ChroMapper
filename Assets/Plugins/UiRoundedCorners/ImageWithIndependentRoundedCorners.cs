using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ImageWithIndependentRoundedCorners : MonoBehaviour
{

    public Vector4 r;
    [SerializeField] private Material material;
    [HideInInspector] public Material mat;
    [Tooltip("When this is false the inserted material will change the inserted material shader.")] public bool cloneMaterial = true;

    // xy - position,
    // zw - halfSize
    [HideInInspector, SerializeField] private Vector4 rect2props;

    private readonly int prop_halfSize = Shader.PropertyToID("_halfSize");
    private readonly int prop_radiuses = Shader.PropertyToID("_r");
    private readonly int prop_rect2props = Shader.PropertyToID("_rect2props");

    // Vector2.right rotated clockwise by 45 degrees
    private static readonly Vector2 wNorm = new Vector2(.7071068f, -.7071068f);
    // Vector2.right rotated counter-clockwise by 45 degrees
    private static readonly Vector2 hNorm = new Vector2(.7071068f, .7071068f);

    public void Refresh()
    {
        if (cloneMaterial)
        {
            mat = new Material(material);
            Image i = GetComponent<Image>();
            i.material = mat;
            i.material.name = "Inherited From Round Corners";
        }
        else
        {
            mat = material;
        }

        Rect rect = ((RectTransform)transform).rect;
        RecalculateProps(rect.size);
        mat.SetVector(prop_rect2props, rect2props);
        mat.SetVector(prop_halfSize, rect.size * .5f);
        mat.SetVector(prop_radiuses, r);
    }

    private void OnRectTransformDimensionsChange()
    {
        Refresh();
    }

    private void OnValidate()
    {
        Refresh();
    }

    private void RecalculateProps(Vector2 size)
    {

        // Vector that goes from left to right sides of rect2
        Vector2 aVec = new Vector2(size.x, -size.y + r.x + r.z);

        // Project vector aVec to wNorm to get magnitude of rect2 width vector
        float halfWidth = Vector2.Dot(aVec, wNorm) * .5f;
        rect2props.z = halfWidth;


        // Vector that goes from bottom to top sides of rect2
        Vector2 bVec = new Vector2(size.x, size.y - r.w - r.y);

        // Project vector bVec to hNorm to get magnitude of rect2 height vector
        float halfHeight = Vector2.Dot(bVec, hNorm) * .5f;
        rect2props.w = halfHeight;


        // Vector that goes from left to top sides of rect2
        Vector2 efVec = new Vector2(size.x - r.x - r.y, 0);
        // Vector that goes from point E to point G, which is top-left of rect2
        Vector2 egVec = hNorm * Vector2.Dot(efVec, hNorm);
        // Position of point E relative to center of coord system
        Vector2 ePoint = new Vector2(r.x - (size.x / 2), size.y / 2);
        // Origin of rect2 relative to center of coord system
        // ePoint + egVec == vector to top-left corner of rect2
        // wNorm * halfWidth + hNorm * -halfHeight == vector from top-left corner to center
        Vector2 origin = ePoint + egVec + wNorm * halfWidth + hNorm * -halfHeight;
        rect2props.x = origin.x;
        rect2props.y = origin.y;
    }
}
