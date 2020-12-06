using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class EnumPicker : MonoBehaviour
{
    public event Action<Enum> OnClick;
    public bool Locked { get; set; }

    [SerializeField] private GameObject optionPrefab;
    [SerializeField] private bool shouldBold = true;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private bool resizeSelected = false;
    [SerializeField] private float selectedSize = 16;
    private float regularSize;

    private Type type;
    private Dictionary<Enum, TextMeshProUGUI> items = new Dictionary<Enum, TextMeshProUGUI>();
    private TextMeshProUGUI lastSelected;
    private TextMeshProUGUI beforeLastSelected;

    public void Initialize(Type type)
    {
        foreach (Enum enumValue in Enum.GetValues(type))
        {
            GameObject option = Instantiate(optionPrefab, optionPrefab.transform.parent);
            TextMeshProUGUI textMesh = option.GetComponent<TextMeshProUGUI>();
            regularSize = textMesh.fontSize;
            textMesh.text = enumValue.ToString();
            PickerChoiceAttribute pickerChoice = GetPickerChoice(enumValue);
            if (pickerChoice != null)
            {
                LocalizeStringEvent localizeString = textMesh.GetComponent<LocalizeStringEvent>();
                if (localizeString == null)
                    throw new Exception($"Enum Picker prefab for type '{type}' is missing LocalizeStringEvent component");
                LocalizedString localizedString = new LocalizedString();
                localizedString.SetReference(pickerChoice.Table, pickerChoice.Entry);
                localizeString.StringReference = localizedString;
            }
            items.Add(enumValue, textMesh);
            option.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (Locked)
                    return;
                Select(textMesh);
                OnClick?.Invoke(enumValue);
            });
            option.SetActive(true);
        }
        TextMeshProUGUI defaultSelected = items.First().Value; //todo maybe add an optional default selected parameter
        foreach (TextMeshProUGUI text in items.Values)
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
        if(lastSelected != null)
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

    private static PickerChoiceAttribute GetPickerChoice(Enum GenericEnum)
    {
        Type genericEnumType = GenericEnum.GetType();
        MemberInfo[] memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
        if ((memberInfo != null && memberInfo.Length > 0))
        {
            var _Attribs = memberInfo[0].GetCustomAttributes(typeof(PickerChoiceAttribute), false);
            if ((_Attribs != null && _Attribs.Count() > 0))
            {
                return ((PickerChoiceAttribute)_Attribs.ElementAt(0));
            }
        }
        return null;
    }

    private IEnumerator InterpolateToSize(TextMeshProUGUI textMesh, float size)
    {
        float originalSize = textMesh.fontSize;
        for(int time = 0; time <= 10; time++)
        {
            textMesh.fontSize = Mathf.Lerp(originalSize, size, Mathf.Pow(time / 10f, 1/3f));
            yield return new WaitForSeconds(1 / 60f);
        }
        textMesh.fontSize = size;
    }

    public class PickerChoiceAttribute : Attribute
    {
        public string Table { get; private set; }
        public string Entry { get; private set; }
        public PickerChoiceAttribute(string table, string entry)
        {
            Table = table;
            Entry = entry;
        }
    }
}
