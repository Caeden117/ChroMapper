using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using EditorGUIUtility = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUIUtility;

[CustomEditor(typeof(BetterSlider)), CanEditMultipleObjects]
public class SliderBuilder : Editor
{
    private bool showHiddenSettings = false;
    
    private BetterSlider _slider;
    
    private void OnEnable()
    {
        _slider = (BetterSlider) target;
    }

    public override void OnInspectorGUI() //Why is this broken on BUILD
    {
        try
        {
            _slider.description.text = EditorGUILayout.TextField("Description", _slider.description.text);
            EditorGUILayout.Separator();

            _slider.slider.wholeNumbers = EditorGUILayout.Toggle("Use Only Whole Numbers?", _slider.slider.wholeNumbers);
            
            EditorGUILayout.BeginHorizontal();
            _slider.slider.minValue = _slider.slider.wholeNumbers ? 
                EditorGUILayout.IntField("Min Slider Value", (int)_slider.slider.minValue) : 
                EditorGUILayout.FloatField("Min Slider Value", _slider.slider.minValue);
            
            _slider.slider.maxValue = _slider.slider.wholeNumbers ? 
                EditorGUILayout.IntField("Max Slider Value", (int)_slider.slider.maxValue) : 
                EditorGUILayout.FloatField("Max Slider Value", _slider.slider.maxValue);
            EditorGUILayout.EndHorizontal();
            
            _slider.defaultSliderValue = _slider.slider.wholeNumbers ? 
                EditorGUILayout.IntSlider("Default Slider Value", (int)_slider.defaultSliderValue, (int)_slider.slider.minValue, (int)_slider.slider.maxValue) : 
                EditorGUILayout.Slider("Default Slider Value", _slider.defaultSliderValue, _slider.slider.minValue, _slider.slider.maxValue);

            _slider.slider.value = _slider.defaultSliderValue;
            
            EditorGUILayout.Separator();

            if (_slider.showPercent) _slider.showValue = false;
            if (_slider.showValue) _slider.showPercent = false;
            
            _slider.showPercent = EditorGUILayout.BeginToggleGroup("Show As Percent", _slider.showPercent);
            EditorGUILayout.Foldout(_slider.showPercent,"Percent Settings");
            if (_slider.showPercent)
            {
                _slider.percentMatchesValues = EditorGUILayout.Toggle("Should the Percent Match Decimal Places?",
                    _slider.percentMatchesValues);
                _slider.multipleOffset = EditorGUILayout.FloatField("Multiple Offset", _slider.multipleOffset);
            }
            EditorGUILayout.EndToggleGroup();
            
            
            _slider.showValue = EditorGUILayout.BeginToggleGroup("Show As Value", _slider.showValue);
            EditorGUILayout.Foldout(_slider.showValue,"Value Settings");
            if (_slider.showValue)
            {
                _slider.multipleOffset = EditorGUILayout.FloatField("Multiple Offset", _slider.multipleOffset);
            }
            EditorGUILayout.EndToggleGroup();
            
            EditorGUILayout.Separator();
            
            _slider.decimalPlaces = EditorGUILayout.IntSlider("How Many Decimal Places Should Be Shown?", _slider.decimalPlaces,0,6);
            
            _slider._decimalsMustMatchForDefault = EditorGUILayout.Toggle("Decimals Must Match To Show Default", _slider._decimalsMustMatchForDefault);
            
            _slider._endTextEnabled = EditorGUILayout.Toggle("Custom End Text Enabled", _slider._endTextEnabled);
            if (_slider._endTextEnabled) _slider._endText = EditorGUILayout.TextField("    -> End Text", _slider._endText);
            
            //serializedObject.Update();
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("slider").FindPropertyRelative("onValueChanged"), true); //Coming soon???
            //serializedObject.ApplyModifiedProperties();


            if (_slider.showPercent)
            {
                _slider.valueText.text = (_slider.defaultSliderValue/_slider.slider.maxValue * 100).ToString("F" + _slider.decimalPlaces);
                
                if (_slider._endTextEnabled) _slider.valueText.text += _slider._endText;
                else _slider.valueText.text += "%";
            }
            else if (_slider.showValue)
            {
                _slider.valueText.text = (_slider.defaultSliderValue*_slider.multipleOffset).ToString("F" + _slider.decimalPlaces);
                if (_slider._endTextEnabled) _slider.valueText.text += _slider._endText;
            }
            
            
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_slider);
                EditorUtility.SetDirty(_slider.description);
                EditorUtility.SetDirty(_slider.slider);
                EditorUtility.SetDirty(_slider.valueText);
            }
        }
        catch (NullReferenceException)
        {
            EditorGUILayout.HelpBox("Error while loading custom editor, showing standard settings.", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}