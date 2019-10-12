using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "EventAppearanceSO", menuName = "Map/Appearance/Event Appearance SO")]
public class EventAppearanceSO : ScriptableObject
{
    [SerializeField] private Vector3 FlashShaderOffset;
    [SerializeField] private Vector3 FadeShaderOffset;
    [Space(5)]
    [SerializeField] private GameObject LaserSpeedPrefab;
    [Space(5)]
    [Header("Default Colors")]
    [SerializeField] private Color RedColor;
    [SerializeField] private Color BlueColor;
    [SerializeField] private Color OffColor;
    [Tooltip("Example: Ring rotate/Ring zoom/Light speed change events")]
    [SerializeField] private Color OtherColor;

    public void SetEventAppearance(BeatmapEventContainer e) {
        Color color = Color.white;
        if (e.GetComponentInChildren<TextMeshProUGUI>()) Destroy(e.GetComponentInChildren<TextMeshProUGUI>().transform.parent.gameObject);
        if (e.eventData._type == MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED || e.eventData._type == MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED) {
            GameObject instantiate = Instantiate(LaserSpeedPrefab, e.transform);
            instantiate.transform.localPosition = new Vector3(0, 0.25f, 0);
            instantiate.GetComponentInChildren<TextMeshProUGUI>().text = e.eventData._value.ToString();
            instantiate.GetComponentInChildren<TextMeshProUGUI>().rectTransform.localScale = new Vector3(2f / 3, 2f / 3, 2f / 3);
        }
        if (e.eventData.IsUtilityEvent())
        {
            e.ChangeColor(OtherColor);
            e.UpdateOffset(Vector3.zero);
            return;
        }
        else
        {
            if (e.eventData._value >= ColourManager.RGB_INT_OFFSET)
            {
                color = ColourManager.ColourFromInt(e.eventData._value);
                e.UpdateAlpha(0.75f);
            }
            else if (e.eventData._value <= 3) color = BlueColor;
            else if (e.eventData._value <= 7) color = RedColor;
        }
        e.ChangeColor(color);
        switch (e.eventData._value)
        {
            case MapEvent.LIGHT_VALUE_OFF:
                e.ChangeColor(OffColor);
                e.UpdateOffset(Vector3.zero);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_FLASH:
                e.UpdateOffset(FlashShaderOffset);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_FADE:
                e.UpdateOffset(FadeShaderOffset);
                break;
            case MapEvent.LIGHT_VALUE_RED_FLASH:
                e.UpdateOffset(FlashShaderOffset);
                break;
            case MapEvent.LIGHT_VALUE_RED_FADE:
                e.UpdateOffset(FadeShaderOffset);
                break;
            case MapEvent.LIGHT_VALUE_RED_ON:
                e.UpdateOffset(Vector3.zero);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_ON:
                e.UpdateOffset(Vector3.zero);
                break;
        }
    }
}
