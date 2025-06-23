using TMPro;
using UnityEngine;

// This is very similar EnvRemovalListItem. Possible to make a shared implementation for this?
public class SongCoreInformationListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField textField;
    private SongCoreInformation controller;
    public string Value { get; private set; }

    private void OnDestroy() => textField.DeactivateInputField();

    public void Setup(SongCoreInformation controllerNew, string v)
    {
        Value = v;
        textField.text = v;
        controller = controllerNew;
    }

    public void OnEndEdit()
    {
        Value = textField.text;
        controller.UpdateSongCoreInfo();
    }

    public void Delete() => controller.Remove(this);
}
