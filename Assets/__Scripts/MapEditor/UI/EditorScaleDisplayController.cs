using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EditorScaleDisplayController : MonoBehaviour {

    [SerializeField] private TextMeshProUGUI text;

	public void UpdateEditorScale(float value)
    {
        text.text = value.ToString();
    }
}
