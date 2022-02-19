using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(BoxSlider), true)]
    [CanEditMultipleObjects]
    public class BoxSliderEditor : SelectableEditor
    {
        private SerializedProperty mHandleRect;
        private SerializedProperty mMaxValue;
        private SerializedProperty mMinValue;
        private SerializedProperty mOnValueChanged;
        private SerializedProperty mValue;
        private SerializedProperty mValueY;
        private SerializedProperty mWholeNumbers;

        protected override void OnEnable()
        {
            base.OnEnable();
            mHandleRect = serializedObject.FindProperty("m_HandleRect");

            mMinValue = serializedObject.FindProperty("m_MinValue");
            mMaxValue = serializedObject.FindProperty("m_MaxValue");
            mWholeNumbers = serializedObject.FindProperty("m_WholeNumbers");
            mValue = serializedObject.FindProperty("m_Value");
            mValueY = serializedObject.FindProperty("m_ValueY");
            mOnValueChanged = serializedObject.FindProperty("m_OnValueChanged");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(mHandleRect);

            if (mHandleRect.objectReferenceValue != null)
            {
                EditorGUI.BeginChangeCheck();


                EditorGUILayout.PropertyField(mMinValue);
                EditorGUILayout.PropertyField(mMaxValue);
                EditorGUILayout.PropertyField(mWholeNumbers);
                EditorGUILayout.Slider(mValue, mMinValue.floatValue, mMaxValue.floatValue);
                EditorGUILayout.Slider(mValueY, mMinValue.floatValue, mMaxValue.floatValue);

                // Draw the event notification options
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(mOnValueChanged);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Specify a RectTransform for the slider fill or the slider handle or both. Each must have a parent RectTransform that it can slide within.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
