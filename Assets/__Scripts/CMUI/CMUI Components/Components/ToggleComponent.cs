using UnityEngine;
using UnityEngine.UI;

public class ToggleComponent : CMUIComponentWithLabel<bool>, INavigable
{
    [SerializeField] private Toggle toggle;

    [field: SerializeField] public Selectable Selectable { get; set; }

    protected override void OnValueUpdated(bool updatedValue) => toggle.SetIsOnWithoutNotify(updatedValue);

    private void Start()
    {
        toggle.onValueChanged.AddListener(ToggleValueChanged);
        OnValueUpdated(Value);
    }

    private void ToggleValueChanged(bool res) => Value = res;

    private void OnDestroy() => toggle.onValueChanged.RemoveAllListeners();
}
