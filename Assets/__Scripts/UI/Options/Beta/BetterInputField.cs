using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BetterInputField : MonoBehaviour
{
    private TextMeshProUGUI _description;
    private TMP_InputField _text;
    [HideInInspector] public bool hasError; //May be used later on
    
    private readonly Color white = new Color(0.7924528f,0.7924528f,0.7924528f);
    
    public void Start()
    {
        _description = GetComponentsInChildren<TextMeshProUGUI>().First(t => t.name == "Label");
        _text = GetComponentInChildren<TMP_InputField>();
    }

    private void LateUpdate()
    {
        _description.color = hasError ? Color.red : white;
    }

    public void Set(string s)
    {
        StartCoroutine(Setup(s));
    }

    private IEnumerator Setup(string s)
    {
        int i = 0;
        while (i<50) //there has to be a better way doing this
        {
            i++;
            yield return new WaitForEndOfFrame();
        }

        _text.text = s;
    }
}
