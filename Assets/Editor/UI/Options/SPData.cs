using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SerializedPropertyViewer : EditorWindow
{
    private static GUIStyle richTextStyle;
    private List<SpData> data;
    private bool dirty = true;

    private Object obj;
    private Vector2 scrollPos;
    private string searchStr;
    private string searchStrRep;

    private void OnGUI()
    {
        if (richTextStyle == null)
            //EditorStyles does not exist in Constructor??
            richTextStyle = new GUIStyle(EditorStyles.label) { richText = true };

        var newObj = EditorGUILayout.ObjectField("Object:", obj, typeof(Object), false);
        var newSearchStr = EditorGUILayout.TextField("Search:", searchStr);
        if (newSearchStr != searchStr)
        {
            searchStr = newSearchStr;
            searchStrRep = "<color=green>" + searchStr + "</color>";
            dirty = true;
        }

        if (obj != newObj)
        {
            obj = newObj;
            dirty = true;
        }

        if (data == null) dirty = true;
        if (dirty)
        {
            dirty = false;
            SearchObject(obj);
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var line in data)
        {
            EditorGUI.indentLevel = line.Depth;
            if (line.Oid > 0) GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(line.Info, richTextStyle);
            if (line.Oid > 0)
            {
                if (GUILayout.Button(">>", GUILayout.Width(50))) Selection.activeInstanceID = line.Oid;

                GUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    [MenuItem("Window/SP Viewer")]
    private static void Init()
    {
        // Get existing open window or if none, make a new one:
        var window = (SerializedPropertyViewer)GetWindow(typeof(SerializedPropertyViewer));
        window.titleContent = new GUIContent("SP Viewer");
        window.Show();
    }

    private void SearchObject(Object obj)
    {
        data = new List<SpData>();
        if (obj == null) return;
        var so = new SerializedObject(obj);
        var iterator = so.GetIterator();
        Search(iterator, 0);
    }

    private void Search(SerializedProperty prop, int depth)
    {
        LOGProperty(prop);
        while (prop.Next(true)) LOGProperty(prop);
    }

    private void LOGProperty(SerializedProperty prop)
    {
        var strVal = GETStringValue(prop);
        var propDesc = prop.propertyPath + " type:" + prop.type + " name:" + prop.name + " val:" + strVal;
        if (searchStr.Length > 0) propDesc = propDesc.Replace(searchStr, searchStrRep);
        data.Add(new SpData(prop.depth, propDesc, strVal, GETObjectID(prop)));
    }

    private int GETObjectID(SerializedProperty prop)
    {
        if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue != null)
            return prop.objectReferenceValue.GetInstanceID();
        return 0;
    }

    private string GETStringValue(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.String:
                return prop.stringValue;
            case SerializedPropertyType.Character: //this isn't really a thing, chars are ints!
            case SerializedPropertyType.Integer:
                if (prop.type == "char") return Convert.ToChar(prop.intValue).ToString();
                return prop.intValue.ToString();
            case SerializedPropertyType.ObjectReference:
                if (prop.objectReferenceValue != null)
                    return prop.objectReferenceValue.ToString();
                else
                    return "(null)";
            default:
                return "";
        }
    }

    public class SpData
    {
        public int Depth;
        public string Info;
        public int Oid;
        public string Val;

        public SpData(int d, string i, string v, int o)
        {
            if (d < 0) d = 0;
            Depth = d;
            Info = i;
            Val = v;
            Oid = o;
        }
    }
}
