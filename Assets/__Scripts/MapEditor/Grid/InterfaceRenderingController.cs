using UnityEngine;

public class InterfaceRenderingController : MonoBehaviour
{
    [SerializeField] private Renderer interfaceRenderer;

    private MaterialPropertyBlock materialPropertyBlock;

    private void Start()
    {
        materialPropertyBlock = new MaterialPropertyBlock();

        Settings.NotifyBySettingName(nameof(Settings.InterfaceOpacity), UpdateSettings);

        UpdateSettings(null);
    }

    private void UpdateSettings(object _)
    {
        materialPropertyBlock.SetColor("_Color", Color.white.WithAlpha(Settings.Instance.InterfaceOpacity));

        interfaceRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.InterfaceOpacity));
    }
}
