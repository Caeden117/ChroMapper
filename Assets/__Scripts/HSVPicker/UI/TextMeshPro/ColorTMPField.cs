﻿using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.HSVPicker.UI.TextMeshPro
{
    [RequireComponent(typeof(TMP_InputField))]
    public class ColorTMPField : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private ColorPicker picker;
        [SerializeField] private ColorValues type;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = 1;
        [SerializeField] private bool clampToValues = true;

        private TMP_InputField input;

        private void Awake() => input = GetComponent<TMP_InputField>();

        private void OnEnable()
        {
            if (Application.isPlaying && picker != null)
            {
                picker.ONValueChanged.AddListener(ColorChanged);
                picker.OnhsvChanged.AddListener(HSVChanged);
                input.onValueChanged.AddListener(TextChanged);
            }
        }

        private void OnDisable()
        {
            if (picker != null)
            {
                picker.ONValueChanged.RemoveListener(ColorChanged);
                picker.OnhsvChanged.RemoveListener(HSVChanged);
                input.onValueChanged.RemoveListener(TextChanged);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            input = GetComponent<TMP_InputField>();
            UpdateValue();
        }
#endif

        public void OnDeselect(BaseEventData eventData) =>
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(ColorTMPField),
                typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));

        public void OnSelect(BaseEventData eventData) =>
            CMInputCallbackInstaller.DisableActionMaps(typeof(ColorTMPField),
                typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));

        private void ColorChanged(Color color) => UpdateValue();

        private void HSVChanged(float hue, float saturation, float value) => UpdateValue();

        private void UpdateValue()
        {
            if (input.isFocused) return;
            if (picker == null)
            {
                input.SetTextWithoutNotify("0");
            }
            else
            {
                var value = (float)Math.Round(picker.GetValue(type), 3);

                if (clampToValues) value = Mathf.Clamp(value, minValue, maxValue);

                input.SetTextWithoutNotify(value.ToString());
            }
        }

        private void TextChanged(string value)
        {
            if (float.TryParse(value, out var v))
            {
                v = (float)Math.Round(v, 3);

                if (clampToValues) v = Mathf.Clamp(v, minValue, maxValue);

                picker.AssignColor(type, v);
            }
        }
    }
}
