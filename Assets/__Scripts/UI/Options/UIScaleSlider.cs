using UnityEngine;
using UnityEngine.UI;

public class UIScaleSlider : BetterSlider
{
    [SerializeField] public CanvasScaler canvasScaler;
    private Vector2 referenceResolution;

    protected override void Start()
    {
        referenceResolution = canvasScaler.referenceResolution;

        base.Start();
    }

    public void OnPointerDown()
    {
        canvasScaler.gameObject.SetActive(true);
    }

    public void OnPointerUp()
    {
        canvasScaler.gameObject.SetActive(false);
        SendMessage("SendValueToSettings", value);
    }

    protected override void UpdateDisplay(bool _)
    {
        canvasScaler.referenceResolution = referenceResolution * value;

        valueString.StringReference.RefreshString();

        if (_decimalsMustMatchForDefault)
            valueText.color = (defaultSliderValue == value) ? new Color(1f, 0.75f, 0.23f) : Color.white;
        else valueText.color = (defaultSliderValue.ToString("F0") == value.ToString("F0")) ? new Color(1f, 0.75f, 0.23f) : Color.white;
    }
}
