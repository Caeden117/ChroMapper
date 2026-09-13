using UnityEngine;

public class Spectrogram : MonoBehaviour
{
    [SerializeField] public bool SetAsGlobal;
    [SerializeField] public MeshRenderer[] MeshRenderers;
    [SerializeField] public MaterialPropertyBlockController MpbController;
    [SerializeField] public SpectrogramDataProvider SpectrogramDataProvider;

    private static readonly int spectrogramDataID = Shader.PropertyToID("_SpectrogramData");
    private static MaterialPropertyBlock materialPropertyBlock;

    private MaterialPropertyBlock MaterialPropertyBlock
    {
        get
        {
            if (!(MpbController != null)) return materialPropertyBlock;
            return MpbController.Mpb;
        }
    }

    protected void Awake()
    {
        if (!SetAsGlobal && MpbController == null && materialPropertyBlock == null)
            materialPropertyBlock = new MaterialPropertyBlock();
    }

    protected void Update()
    {
        if (!SpectrogramDataProvider.HasInitialized) return;
        if (SetAsGlobal)
        {
            Shader.SetGlobalFloatArray(spectrogramDataID, SpectrogramDataProvider.ProcessedSamples);
            return;
        }

        MaterialPropertyBlock.SetFloatArray(spectrogramDataID, SpectrogramDataProvider.ProcessedSamples);
        if (MpbController != null)
        {
            MpbController.ApplyChanges();
            return;
        }

        foreach (var t in MeshRenderers) t.SetPropertyBlock(materialPropertyBlock);
    }
}
