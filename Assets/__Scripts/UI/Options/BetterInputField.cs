using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BetterInputField : MonoBehaviour
{
    public  TextMeshProUGUI _description;
    [SerializeField] private TMP_InputField _text;
    [HideInInspector] public bool hasError; //May be used later on
    
    private readonly Color white = new Color(0.7924528f,0.7924528f,0.7924528f);
    
    public string text
    {
        get => _text.text;
        set
        {
            _text.text = value;
        }
    }

    private void Start()
    {
        _text.onValueChanged.AddListener(OnValueChanged);
        _text.SetTextWithoutNotify(GetComponent<SettingsBinder>()?.RetrieveValueFromSettings().ToString() ?? "");
    }

    private void OnValueChanged(string value)
    {
        SendMessage("SendValueToSettings", value);
    }
}