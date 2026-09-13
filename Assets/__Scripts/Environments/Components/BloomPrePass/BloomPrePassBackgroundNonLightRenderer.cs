using UnityEngine;

public class BloomPrePassBackgroundNonLightRenderer : BloomPrePassBackgroundNonLightRendererCore
{
    [SerializeField] public MeshFilter MeshFilter;

    public Transform CachedTransform;
    private bool isPartOfInstancedRendering;

    public bool IsPartOfInstancedRendering
    {
        set
        {
            if (value)
                Unregister();
            else
                Register();
            isPartOfInstancedRendering = value;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        CachedTransform = transform;
    }

    protected override void OnEnable()
    {
        if (!isPartOfInstancedRendering) base.OnEnable();
    }

    protected override void OnValidate()
    {
        if (isActiveAndEnabled && !isPartOfInstancedRendering)
            Register();
        else
            Unregister();
    }

    public void SetRenderer(Renderer renderer) => Renderer = renderer;

    protected override void InitIfNeeded()
    {
        if (!isPartOfInstancedRendering)
            base.InitIfNeeded();
        else if (!(Renderer == null) && !KeepDefaultRendering) Renderer.enabled = false;
    }
}
