using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class SerializedPropertyViewer : EditorWindow
{

  public class SPData
  {
    public int depth;
    public string info;
    public string val;
    public int oid;

    public SPData(int d, string i, string v, int o)
    {
      if(d < 0)
      {
        d = 0;
      }
      depth = d;
      info = i;
      val = v;
      oid = o;
    }
  }

  [MenuItem ("Window/SP Viewer")]
  static void Init () 
  {
    // Get existing open window or if none, make a new one:
    SerializedPropertyViewer window = (SerializedPropertyViewer)EditorWindow.GetWindow (typeof (SerializedPropertyViewer));
    window.titleContent = new GUIContent("SP Viewer");
    window.Show();
  }

  UnityEngine.Object obj;

  Vector2 scrollPos;
  List<SPData> data;
  bool dirty = true;
  string searchStr;
  string searchStrRep;

  public static GUIStyle richTextStyle;


  void OnGUI () 
  {
    if(richTextStyle == null)
    {
      //EditorStyles does not exist in Constructor??
      richTextStyle = new GUIStyle(EditorStyles.label);
      richTextStyle.richText = true;
    }

    UnityEngine.Object newObj = EditorGUILayout.ObjectField("Object:", obj, typeof(UnityEngine.Object), false);
    string newSearchStr = EditorGUILayout.TextField("Search:", searchStr);
    if(newSearchStr != searchStr)
    {
      searchStr = newSearchStr;
      searchStrRep = "<color=green>"+searchStr+"</color>";
      dirty = true;
    }
    if(obj != newObj)
    {
      obj = newObj;
      dirty = true;
    }
    if(data == null)
    {
      dirty = true;
    }
    if(dirty == true)
    {
      dirty = false;
      searchObject(obj);
    }
    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
    foreach(SPData line in data)
    {
      EditorGUI.indentLevel = line.depth;
      if(line.oid > 0){
        GUILayout.BeginHorizontal();

      }
      EditorGUILayout.LabelField(line.info, richTextStyle);
      if(line.oid > 0)
      {
        if(GUILayout.Button(">>", GUILayout.Width(50)))
        {
          Selection.activeInstanceID = line.oid;
        }

        GUILayout.EndHorizontal();
      }
    }
    EditorGUILayout.EndScrollView();

  }

  void searchObject(UnityEngine.Object obj)
  {
    data = new List<SPData>();
    if(obj == null)
    {
      return;
    }
    SerializedObject so = new SerializedObject(obj);
    SerializedProperty iterator = so.GetIterator();
    search(iterator, 0);
  }

  void search(SerializedProperty prop, int depth)
  {
    logProperty(prop);
    while(prop.Next(true))
    {
      logProperty(prop);
    }
  }


  void logProperty(SerializedProperty prop)
  {
    string strVal = getStringValue(prop);
    string propDesc = prop.propertyPath+" type:"+prop.type + " name:"+prop.name + " val:"+ strVal;
    if(searchStr.Length > 0)
    {
      propDesc = propDesc.Replace(searchStr, searchStrRep);
    }
    data.Add(new SPData(prop.depth, propDesc, strVal, getObjectID(prop)));
  }

  int getObjectID(SerializedProperty prop)
  {
    if(prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue != null)
    {
      return prop.objectReferenceValue.GetInstanceID();
    }
    return 0;
  }

  string getStringValue(SerializedProperty prop)
  {
    switch(prop.propertyType)
    {
      case SerializedPropertyType.String:
        return prop.stringValue;
      case SerializedPropertyType.Character: //this isn't really a thing, chars are ints!
      case SerializedPropertyType.Integer:
        if(prop.type == "char")
        {
          return System.Convert.ToChar(prop.intValue).ToString();
        }
        return prop.intValue.ToString();
      case SerializedPropertyType.ObjectReference:
      if(prop.objectReferenceValue != null)
      {
        return prop.objectReferenceValue.ToString();
      }else{
        return "(null)";
      }
      default:
        return "";
    }
  }


}