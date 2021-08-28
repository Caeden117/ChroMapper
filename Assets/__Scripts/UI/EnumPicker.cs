using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class EnumPicker : MonoBehaviour
{
    [SerializeField] private GameObject optionPrefab;
    [SerializeField] private bool shouldBold = true;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private bool resizeSelected;
    [SerializeField] private float selectedSize = 16;
    private readonly Dictionary<Enum, TextMeshProUGUI> items = new Dictionary<Enum, TextMeshProUGUI>();

    private TextMeshProUGUI beforeLastSelected;
    private TextMeshProUGUI lastSelected;
    private float regularSize;
    public bool Locked { get; set; }
    public event Action<Enum> Click;

    public void Initialize(Type type)
    {
        foreach (Enum enumValue in Enum.GetValues(type))
        {
            var option = Instantiate(optionPrefab, optionPrefab.transform.parent);
            var textMesh = option.GetComponent<TextMeshProUGUI>();
            regularSize = textMesh.fontSize;
            textMesh.text = enumValue.ToString();
            var pickerChoice = GetPickerChoice(enumValue);
            if (pickerChoice != null)
            {
                var localizeString = textMesh.GetComponent<LocalizeStringEvent>();
                if (localizeString == null)
                {
                    throw new Exception(
                        $"Enum Picker prefab for type '{type}' is missing LocalizeStringEvent component");
                }

                var localizedString = new LocalizedString();
                localizedString.SetReference(pickerChoice.Table, pickerChoice.Entry);
                localizeString.StringReference = localizedString;
            }

            items.Add(enumValue, textMesh);
            option.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (Locked)
                    return;
                Select(textMesh);
                Click?.Invoke(enumValue);
            });
            option.SetActive(true);
        }

        var defaultSelected = items.First().Value; //todo maybe add an optional default selected parameter
        foreach (var text in items.Values)
        {
            if (shouldBold)
                text.fontStyle &= ~FontStyles.Bold;
            text.color = normalColor;
        }

        Select(defaultSelected);
    }

    private void Select(TextMeshProUGUI toSelect)
    {
        StopAllCoroutines();
        if (beforeLastSelected != null && beforeLastSelected != toSelect && resizeSelected)
            beforeLastSelected.fontSize = regularSize;
        if (lastSelected != null)
        {
            if (shouldBold)
                lastSelected.fontStyle &= ~FontStyles.Bold;
            lastSelected.color = normalColor;
            if (resizeSelected)
                StartCoroutine(InterpolateToSize(lastSelected, regularSize));
        }

        if (shouldBold)
            toSelect.fontStyle |= FontStyles.Bold;
        toSelect.color = selectedColor;
        if (resizeSelected)
            StartCoroutine(InterpolateToSize(toSelect, selectedSize));
        beforeLastSelected = lastSelected;
        lastSelected = toSelect;
    }

    public void Select(Enum enumValue) => Select(items[enumValue]);

    private static PickerChoiceAttribute GetPickerChoice(Enum genericEnum)
    {
        var genericEnumType = genericEnum.GetType();
        var memberInfo = genericEnumType.GetMember(genericEnum.ToString());
        if (memberInfo != null && memberInfo.Length > 0)
        {
            var attribs = memberInfo[0].GetCustomAttributes(typeof(PickerChoiceAttribute), false);
            if (attribs != null && attribs.Count() > 0) return (PickerChoiceAttribute)attribs.ElementAt(0);
        }

        return null;
    }

    private IEnumerator InterpolateToSize(TextMeshProUGUI textMesh, float size)
    {
        var originalSize = textMesh.fontSize;
        for (var time = 0; time <= 10; time++)
        {
            textMesh.fontSize = Mathf.Lerp(originalSize, size, Mathf.Pow(time / 10f, 1 / 3f));
            yield return new WaitForSeconds(1 / 60f);
        }

        textMesh.fontSize = size;
    }

    public class PickerChoiceAttribute : Attribute
    {
        public PickerChoiceAttribute(string table, string entry)
        {
            Table = table;
            Entry = entry;
        }

        public string Table { get; }
        public string Entry { get; }
    }
}
