using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class BetterInputField : MonoBehaviour
{
    [FormerlySerializedAs("_description")] public TextMeshProUGUI Description;
    [FormerlySerializedAs("_text")] [SerializeField] private TMP_InputField text;
    [FormerlySerializedAs("hasError")] [HideInInspector] public bool HasError; //May be used later on


    public string Text
    {
        get => text.text;
        set => text.text = value;
    }

    private void Start()
    {
        if (TryGetComponent<SettingsBinder>(out var settingsBinder))
        {
            text.onValueChanged.AddListener(OnValueChanged);
            text.SetTextWithoutNotify(settingsBinder.RetrieveValueFromSettings().ToString() ?? "");
        }
    }

    private void OnValueChanged(string value) => SendMessage("SendValueToSettings", value);
}
