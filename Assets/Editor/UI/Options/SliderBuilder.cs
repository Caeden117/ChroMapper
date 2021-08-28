using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BetterSlider))]
[CanEditMultipleObjects]
public class SliderBuilder : Editor
{
    private BetterSlider slider;
    private bool showHiddenSettings;

    private void OnEnable() => slider = (BetterSlider)target;

    public override void OnInspectorGUI() //Why is this broken on BUILD
    {
        try
        {
            slider.Description.text = EditorGUILayout.TextField("Description", slider.Description.text);
            EditorGUILayout.Separator();

            slider.Slider.wholeNumbers =
                EditorGUILayout.Toggle("Use Only Whole Numbers?", slider.Slider.wholeNumbers);

            EditorGUILayout.BeginHorizontal();
            slider.Slider.minValue = slider.Slider.wholeNumbers
                ? EditorGUILayout.IntField("Min Slider Value", (int)slider.Slider.minValue)
                : EditorGUILayout.FloatField("Min Slider Value", slider.Slider.minValue);

            slider.Slider.maxValue = slider.Slider.wholeNumbers
                ? EditorGUILayout.IntField("Max Slider Value", (int)slider.Slider.maxValue)
                : EditorGUILayout.FloatField("Max Slider Value", slider.Slider.maxValue);
            EditorGUILayout.EndHorizontal();

            slider.DefaultSliderValue = slider.Slider.wholeNumbers
                ? EditorGUILayout.IntSlider("Default Slider Value", (int)slider.DefaultSliderValue,
                    (int)slider.Slider.minValue, (int)slider.Slider.maxValue)
                : EditorGUILayout.Slider("Default Slider Value", slider.DefaultSliderValue, slider.Slider.minValue,
                    slider.Slider.maxValue);

            slider.Slider.value = slider.DefaultSliderValue;

            EditorGUILayout.Separator();

            if (slider.ShowPercent) slider.ShowValue = false;
            if (slider.ShowValue) slider.ShowPercent = false;

            slider.ShowPercent = EditorGUILayout.BeginToggleGroup("Show As Percent", slider.ShowPercent);
            EditorGUILayout.Foldout(slider.ShowPercent, "Percent Settings");
            if (slider.ShowPercent)
            {
                slider.PercentMatchesValues = EditorGUILayout.Toggle("Should the Percent Match Decimal Places?",
                    slider.PercentMatchesValues);
                slider.MultipleOffset = EditorGUILayout.FloatField("Multiple Offset", slider.MultipleOffset);
            }

            EditorGUILayout.EndToggleGroup();


            slider.ShowValue = EditorGUILayout.BeginToggleGroup("Show As Value", slider.ShowValue);
            EditorGUILayout.Foldout(slider.ShowValue, "Value Settings");
            if (slider.ShowValue)
                slider.MultipleOffset = EditorGUILayout.FloatField("Multiple Offset", slider.MultipleOffset);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Separator();

            slider.DecimalPlaces =
                EditorGUILayout.IntSlider("How Many Decimal Places Should Be Shown?", slider.DecimalPlaces, 0, 6);

            slider.DecimalsMustMatchForDefault = EditorGUILayout.Toggle("Decimals Must Match To Show Default",
                slider.DecimalsMustMatchForDefault);

            if (slider.ShowPercent)
            {
                slider.ValueText.text =
                    (slider.DefaultSliderValue / slider.Slider.maxValue * 100).ToString("F" + slider.DecimalPlaces);

                slider.ValueText.text += "%";
            }
            else if (slider.ShowValue)
            {
                slider.ValueText.text =
                    (slider.DefaultSliderValue * slider.MultipleOffset).ToString("F" + slider.DecimalPlaces);
            }


            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(slider);
                EditorUtility.SetDirty(slider.Description);
                EditorUtility.SetDirty(slider.Slider);
                EditorUtility.SetDirty(slider.ValueText);
            }
        }
        catch (NullReferenceException)
        {
            EditorGUILayout.HelpBox("Error while loading custom editor, showing standard settings.", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}
