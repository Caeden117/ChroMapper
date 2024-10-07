using System;
using System.Linq;
using SimpleJSON;

[Serializable]
public class EditorsObject
{
    private static string editorName;
    private static string editorVersion;
    private readonly JSONNode editorsObject;

    /// <summary>
    ///     Editor Metadata for this editor.
    /// </summary>
    public JSONNode EditorMetadata = new JSONObject();

    public EditorsObject(JSONNode obj)
    {
        if (obj is null || !obj.Children.Any())
        {
            editorsObject = new JSONObject();
        }
        else
        {
            editorsObject = obj;
            if (editorsObject.HasKey(editorName)) EditorMetadata = editorsObject[editorName];
        }
    }

    public JSONNode ToJsonNode()
    {
        EditorMetadata["version"] = editorVersion;

        editorsObject["_lastEditedBy"] = editorName;
        editorsObject[editorName] = EditorMetadata;

        return editorsObject;
    }
}

