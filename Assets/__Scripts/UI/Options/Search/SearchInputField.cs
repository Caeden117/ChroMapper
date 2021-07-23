using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchInputField : MonoBehaviour
{
     public TMP_InputField InputField;
     public List<SearchableTab> Tabs;
     [SerializeField] private Button clearbutton;
     [SerializeField] private OptionsKeybindsLoader keybindsLoader;

     public void OnValueChanged(string newValue)
     {
          keybindsLoader.OnTabSelected();
          clearbutton.gameObject.SetActive(newValue.Length > 0);
          Tabs.ForEach(it => it.UpdateSearch(newValue));
     }

     public void Clear()
     {
          InputField.text = "";
     }
}