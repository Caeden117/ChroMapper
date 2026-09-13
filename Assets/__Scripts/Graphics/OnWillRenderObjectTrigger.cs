using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class OnWillRenderObjectTrigger : MonoBehaviour
{
    [SerializeField] private Shader overrideShader;
    [SerializeField] private int renderQueue = 3002;

    private Material material;
    private Mesh mesh;

    private void OnEnable()
    {
        if (material == null)
        {
            material = new Material(overrideShader != null ? overrideShader : Shader.Find("Diffuse"))
            {
                name = "GrabPassTexture1",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = renderQueue
            };
        }

        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "Huge Mesh",
                hideFlags = HideFlags.HideAndDontSave,
                bounds = new Bounds(Vector3.zero, Vector3.one * 9999999f)
            };
        }

        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshRenderer.allowOcclusionWhenDynamic = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
    }

    private void OnDisable()
    {
        DestroyGeneratedObject(material);
        DestroyGeneratedObject(mesh);
        material = null;
        mesh = null;
    }

    private static void DestroyGeneratedObject(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Object.Destroy(target);
        else Object.DestroyImmediate(target);
    }
}
