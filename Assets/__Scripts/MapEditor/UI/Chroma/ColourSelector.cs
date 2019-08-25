using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColourSelector : MonoBehaviour {
    
    [SerializeField] private Image ColourSelectorImage;
    [SerializeField] private Image PickedColourResult;
    [SerializeField] private RectTransform PickerLocationTransform;
    private Material HueImageMaterial;

    internal static bool IsHovering = false;
    public static Color SelectedColor = Color.white;

    void Start()
    {
        HueImageMaterial = new Material(ColourSelectorImage.material);
        ColourSelectorImage.material = HueImageMaterial;
    }

    public void UpdateHue(float value)
    {
        HueImageMaterial.SetFloat("_Hue", value);
    }
    
    void Update()
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(ColourSelectorImage.rectTransform, Input.mousePosition))
        {
            PickerLocationTransform.gameObject.SetActive(true);
            PickerLocationTransform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z);
        }
        else PickerLocationTransform.gameObject.SetActive(false);
        if (IsHovering && Input.GetMouseButton(0)) StartCoroutine(GetColour());
    }

    private IEnumerator GetColour()
    {
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        yield return new WaitForEndOfFrame();
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();
        Color pickedColor = tex.GetPixel(Mathf.RoundToInt(Input.mousePosition.x), Mathf.RoundToInt(Input.mousePosition.y));
        UpdateColour(pickedColor);
        Destroy(tex);
    }

    public void UpdateColour(Color color) {
        PickedColourResult.color = color;
        EventPreview.QueuedChromaColor = ColourManager.ColourToInt(color);
    }
}
