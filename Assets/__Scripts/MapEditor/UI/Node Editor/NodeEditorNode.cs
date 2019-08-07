using SimpleJSON;
using UnityEngine;
using TMPro;

public class NodeEditorNode : MonoBehaviour {

    public TMP_InputField KeyInputField;
    public TMP_InputField ValueInputField;

    public string Key;
    public JSONNode Value;

    public void SetValues(string key, JSONNode value, bool fromCustomData = false)
    {
        Key = key;
        Value = value;
        KeyInputField.text = key;
        KeyInputField.interactable = fromCustomData;
        ValueInputField.text = value.Value;
    }

}
