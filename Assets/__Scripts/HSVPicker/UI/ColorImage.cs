using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ColorImage : MonoBehaviour
{
    [FormerlySerializedAs("picker")] public ColorPicker Picker;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        Picker.ONValueChanged.AddListener(ColorChanged);
    }

    private void OnDestroy() => Picker.ONValueChanged.RemoveListener(ColorChanged);

    private void ColorChanged(Color newColor) => image.color = newColor;
}
