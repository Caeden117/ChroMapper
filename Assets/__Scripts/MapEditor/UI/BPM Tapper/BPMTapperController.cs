using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class BpmTapperController : MonoBehaviour, CMInput.IBPMTapperActions
{
    public static bool IsActive;
    private static bool swap;
    [FormerlySerializedAs("_bpmText")] [SerializeField] private TextMeshProUGUI bpmText;
    private readonly List<float> taps = new List<float>();
    private bool isTapping;
    private float t1;

    private float timeSinceLastTap;

    public void Reset()
    {
        isTapping = false;
        StopAllCoroutines();
        bpmText.text = "Tap...";
        taps.Clear();
    }

    private void Start() => bpmText.text = "";

    public void OnToggleBPMTapper(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (UIMode.SelectedMode != UIModeType.Normal) return;
            swap = !swap;

            StopAllCoroutines();
            StartCoroutine(UpdateGroup(swap, transform as RectTransform));
        }
    }

    public void Close()
    {
        swap = false;
        StartCoroutine(UpdateGroup(swap, transform as RectTransform));
    }

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        float dest = enabled ? 120 : -200;

        var og = group.anchoredPosition.y;
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

    public void Tap()
    {
        timeSinceLastTap = 0;
        if (!isTapping)
        {
            isTapping = true;

            StartCoroutine(WaitForReset());

            bpmText.text = "Tap...";

            t1 = Time.time;
        }
        else
        {
            var dist = Time.time - t1;
            t1 = Time.time;
            taps.Add(dist);
            bpmText.text = Math.Round(CalculateBpm(), 2).ToString();
        }
    }

    private float CalculateBpm()
    {
        var avg = taps.Average();
        return 1000 / (avg * 1000) * 60;
    }

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
