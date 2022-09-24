using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BetterToggle : UIBehaviour, IPointerClickHandler
{
    private const float slideSpeed = 0.2f;
    [FormerlySerializedAs("background")] public Image Background;
    [FormerlySerializedAs("switchTransform")] public RectTransform SwitchTransform;
    [FormerlySerializedAs("description")] public TextMeshProUGUI Description;

    [FormerlySerializedAs("isOn")] public bool IsOn; // TODO: Make this property update UI?

    [FormerlySerializedAs("OnColor")] [HideInInspector] public Color Color;
    [HideInInspector] public Color OffColor;

    [FormerlySerializedAs("onValueChanged")] public ToggleEvent OnValueChanged = new ToggleEvent();

    private readonly Vector3 offPos = new Vector3(-35, 0, 0); //No idea why these are the numbers.
    private readonly Vector3 onPos = new Vector3(-15, 0, 0);

    private Coroutine slideButtonCoroutine;
    private Coroutine slideColorCoroutine;

    protected override void Start()
    {
        if (TryGetComponent<SettingsBinder>(out var settingsBinder))
        {
            IsOn = (bool?)settingsBinder.RetrieveValueFromSettings() ?? false;
            UpdateUI();
        }

        base.Start();
    }

    public void UpdateUI()
    {
        SwitchTransform.localPosition = IsOn ? onPos : offPos;
        Background.color = IsOn ? Color : OffColor;
    }

    public void SetUiOn(bool isOn, bool notifyChange = true)
    {
        IsOn = isOn;
        slideButtonCoroutine = StartCoroutine(SlideToggle());
        slideColorCoroutine = StartCoroutine(SlideColor());

        if (!notifyChange) return;
        OnValueChanged?.Invoke(IsOn);
        SendMessage("SendValueToSettings", IsOn, SendMessageOptions.DontRequireReceiver);
    }
    
    public void OnPointerClick(PointerEventData eventData) => SetUiOn(!IsOn);

    private IEnumerator SlideToggle()
    {
        if (slideButtonCoroutine != null) StopCoroutine(slideButtonCoroutine);

        var startTime = Time.time;

        while (true)
        {
            var localPosition = SwitchTransform.localPosition;
            localPosition = Vector3.Lerp(localPosition, IsOn ? onPos : offPos, Time.time / startTime * slideSpeed);
            SwitchTransform.localPosition = localPosition;
            if (SwitchTransform.localPosition == onPos || SwitchTransform.localPosition == offPos) break;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator SlideColor()
    {
        if (slideColorCoroutine != null) StopCoroutine(slideColorCoroutine);

        var startTime = Time.time;

        while (true)
        {
            var color = Background.color;
            color = Color.Lerp(color, IsOn ? Color : OffColor, Time.time / startTime * slideSpeed);
            Background.color = color;
            if (Background.color == Color || Background.color == OffColor) break;
            yield return new WaitForFixedUpdate();
        }
    }

    [Serializable]
    public class ToggleEvent : UnityEvent<bool>
    {
    }
}
