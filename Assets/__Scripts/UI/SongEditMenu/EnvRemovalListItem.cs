using TMPro;
using UnityEngine;

public class EnvRemovalListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField textField;
    public string Value => textField.text;
    private EnvRemoval controller;

    public void Setup(EnvRemoval controllerNew, string v)
    {
        textField.text = v ?? "";
        controller = controllerNew;
    }

    public void OnEndEdit()
    {
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
