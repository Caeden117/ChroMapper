using TMPro;
using UnityEngine;

public class EnvRemovalListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField textField;
    public EnvEnhancement Value { get; private set; }
    private EnvRemoval controller;

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

    public void Delete()
    {
        controller.Remove(this);
    }

    private void OnDestroy()
    {
        textField.DeactivateInputField();
    }
}
