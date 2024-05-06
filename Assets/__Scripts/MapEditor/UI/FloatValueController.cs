using TMPro;
using UnityEngine;

public class FloatValueController : DisableActionsField
{
    [PickerChoice("Mapper", "bar.events.floatValue")]
    [SerializeField] private TMP_InputField floatValue;

    [SerializeField] private EventPlacement eventPlacement;

    public void UpdateManualFloatValue(string result)
    {
        if (int.TryParse(result, out var val))
        {
            eventPlacement.UpdateFloatValue(val / 100f);
        }
    }
}
