using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BPMTapperController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _bpmText;

    public static bool IsActive;
    private static bool Swap;

    private void Start()
    {
        _bpmText.text = "";
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            if(UIMode.SelectedMode != UIModeType.NORMAL) return;
            Swap = !Swap;

            StopAllCoroutines();
            StartCoroutine(UpdateGroup(Swap, transform as RectTransform));
        }
    }
    
    public void Close()
    {
        Swap = false;
        StartCoroutine(UpdateGroup(Swap, transform as RectTransform));
    }

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        float dest = enabled ? 120 : -200;

        float og = group.anchoredPosition.y;
        float t = 0;
        while (t < 0.4)
        {
            t += Time.deltaTime;
            group.anchoredPosition = new Vector2(group.anchoredPosition.x, Mathf.Lerp(og, dest, t));
            og = group.anchoredPosition.y;
            yield return new WaitForEndOfFrame();
        }
        if (!enabled) Reset();
        group.anchoredPosition = new Vector2(group.anchoredPosition.x, dest);
        IsActive = enabled;
    }

    public void Reset()
    {
        isTapping = false;
        StopAllCoroutines();
        _bpmText.text = "Tap...";
        taps.Clear();
    }

    public void Tap()
    {
        timeSinceLastTap = 0;
        if (!isTapping)
        {
            isTapping = true;
            
            StartCoroutine(WaitForReset());

            _bpmText.text = "Tap...";

            t1 = Time.time;
        }
        else
        {
            float dist = Time.time - t1;
            t1 = Time.time;
            taps.Add(dist);
            _bpmText.text = Math.Round(CalculateBPM(), 2).ToString();
        }
    }

    private float CalculateBPM()
    {
        var avg = taps.Average();
        return (1000 / (avg * 1000)) * 60;
    }


    float timeSinceLastTap = 0;
    bool isTapping = false;
    List<float> taps = new List<float>();
    bool firstTap = false;
    float t1 = 0;

    private IEnumerator WaitForReset()
    {
        while (timeSinceLastTap < 3)
        {
            timeSinceLastTap += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        Reset();
    }
}
