using TMPro;
using UnityEngine;

public class EnvRemovalListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField textField;
    private EnvRemoval controller;
    public EnvEnhancement Value { get; private set; }

    private void OnDestroy() => textField.DeactivateInputField();

    public void Setup(EnvRemoval controllerNew, EnvEnhancement v)
    {
        Value = v;
        textField.text = v.ID ?? "";
        controller = controllerNew;
    }

    public void OnEndEdit()
    {
        Value.ID = textField.text;
        controller.UpdateEnvRemoval();
    }

    public void Delete() => controller.Remove(this);
}
