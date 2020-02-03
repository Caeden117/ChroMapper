using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dbValueText;
    private Slider _slider;
    
    private void Awake()
    {
        _slider = GetComponentInChildren<Slider>();
        _slider.onValueChanged.AddListener(OnHandleMove);
    }

    public void Set(float f)
    {
        StartCoroutine(Setup(f));
    }

    private IEnumerator Setup(float f)
    {
        int i = 0;
        while (i<50) //there has to be a better way doing this
        {
            i++;
            yield return new WaitForEndOfFrame();
        }
        
        _slider.value = f;
        UpdateDisplay();
    }
    
    private void OnHandleMove(float value)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        dbValueText.text = _slider.value == 0f ? "Off" : (20.0f * Mathf.Log10(_slider.value)).ToString("F0") + " dB";
    }
}
