using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class TMPEnumPicker : EnumPicker<TextMeshProUGUI>
{
    [SerializeField] private GameObject optionPrefab;
    [SerializeField] private bool shouldBold = true;
    [SerializeField] private bool resizeSelected = false;
    [SerializeField] private float selectedSize = 16;
    private float regularSize;
    private TextMeshProUGUI lastSelected;
    private TextMeshProUGUI beforeLastSelected;

    public override void CreateOptionForEnumValue(Enum enumValue)
    {
        GameObject option = Instantiate(optionPrefab, optionPrefab.transform.parent);

        TextMeshProUGUI textMesh = option.GetComponent<TextMeshProUGUI>();
        regularSize = textMesh.fontSize;
        textMesh.text = enumValue.ToString();
        textMesh.color = normalColor;

        if (shouldBold)
            textMesh.fontStyle &= ~FontStyles.Bold;

        PickerChoiceAttribute pickerChoice = GetPickerChoice(enumValue);
        if (pickerChoice != null)
        {
            LocalizeStringEvent localizeString = textMesh.GetComponent<LocalizeStringEvent>();
            if (localizeString == null)
                throw new Exception($"Enum Picker prefab for type '{enumValue.GetType().Name}' is missing LocalizeStringEvent component");
            
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
            OnEnumValueSelected(enumValue);
        });
        option.SetActive(true);
    }

    protected override void Select(TextMeshProUGUI toSelect)
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

    private IEnumerator InterpolateToSize(TextMeshProUGUI textMesh, float size)
    {
        float originalSize = textMesh.fontSize;
        for (int time = 0; time <= 10; time++)
        {
            textMesh.fontSize = Mathf.Lerp(originalSize, size, Mathf.Pow(time / 10f, 1 / 3f));
            yield return new WaitForSeconds(1 / 60f);
        }
        textMesh.fontSize = size;
    }
}
