using UnityEngine;

public class MeasureLinesRenderingOrderController : MonoBehaviour
{
    [SerializeField] private Canvas effectingCanvas;

    // Start is called before the first frame update
    private void Start()
    {
        Settings.NotifyBySettingName("MeasureLinesShowOnTop", UpdateCanvasOrder);
        UpdateCanvasOrder(Settings.Instance.MeasureLinesShowOnTop);
    }

    // Update is called once per frame
    private void OnDestroy() => Settings.ClearSettingNotifications("MeasureLinesShowOnTop");

    private void UpdateCanvasOrder(object obj) =>
        effectingCanvas.sortingLayerName = (bool)obj ? "Background" : "Default";
}
