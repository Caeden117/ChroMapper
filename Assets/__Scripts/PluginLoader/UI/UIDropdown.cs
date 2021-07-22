using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIDropdown : MonoBehaviour
{
    public RectTransform Transform;
    public TMP_Dropdown Dropdown;

    public void SetOptions(List<string> options)
    {
        Dropdown.ClearOptions();
        Dropdown.AddOptions(options);
    }
}