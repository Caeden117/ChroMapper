using UnityEngine;
using UnityEngine.Serialization;

public class ColorPickerTester : MonoBehaviour
{
#pragma warning disable CS0109 // The member 'ColorPickerTester.Renderer' does not hide an accessible member. The new keyword is not required.
    [FormerlySerializedAs("renderer")] public new Renderer Renderer;
#pragma warning restore CS0109 // The member 'ColorPickerTester.Renderer' does not hide an accessible member. The new keyword is not required.
    [FormerlySerializedAs("picker")] public ColorPicker Picker;

    public Color Color = Color.red;

    // Use this for initialization
    private void Start()
    {
        Picker.ONValueChanged.AddListener(color =>
        {
            Renderer.material.color = color;
            Color = color;
        });

        Renderer.material.color = Picker.CurrentColor;

        Picker.CurrentColor = Color;
    }
}
