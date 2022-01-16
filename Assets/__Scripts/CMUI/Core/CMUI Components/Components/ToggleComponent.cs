using UnityEngine;
using UnityEngine.UI;

public class ToggleComponent : CMUIComponentWithLabel<bool>
{
    [SerializeField] private Toggle toggle;

    protected override void OnValueUpdated(bool updatedValue) => toggle.SetIsOnWithoutNotify(updatedValue);

    private void Start()
    {
        toggle.onValueChanged.AddListener(ToggleValueChanged);
        OnValueUpdated(Value);
    }

    private void ToggleValueChanged(bool res) => Value = res;

    private void OnDestroy() => toggle.onValueChanged.RemoveAllListeners();
}
