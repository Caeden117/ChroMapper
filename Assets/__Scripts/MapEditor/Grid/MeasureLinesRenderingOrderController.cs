using UnityEngine;

public class MeasureLinesRenderingOrderController : MonoBehaviour
{
    [SerializeField] private Canvas effectingCanvas;

    // Start is called before the first frame update
    void Start()
    {
        Settings.NotifyBySettingName("MeasureLinesShowOnTop", UpdateCanvasOrder);
        UpdateCanvasOrder(Settings.Instance.MeasureLinesShowOnTop);
    }

    private void UpdateCanvasOrder(object obj)
    {
        effectingCanvas.sortingLayerName = (bool)obj ? "Background" : "Default";
    }

    // Update is called once per frame
    void OnDestroy()
    {
        Settings.ClearSettingNotifications("MeasureLinesShowOnTop");
    }
}
