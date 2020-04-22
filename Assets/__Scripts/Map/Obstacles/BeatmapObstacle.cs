using SimpleJSON;
using System;

[Serializable]
public class BeatmapObstacle : BeatmapObject {

    //These are uhh, assumptions...
    public const int VALUE_FULL_BARRIER = 0;
    public const int VALUE_HIGH_BARRIER = 1;

    /*
     * Obstacle Logic
     */

    public BeatmapObstacle(JSONNode node) {
        _time = node["_time"].AsFloat; //Get rid of floating precision errors
        _lineIndex = node["_lineIndex"].AsInt;
        _type = node["_type"].AsInt;
        _duration = float.Parse(node["_duration"].AsFloat.ToString("0.000")); //Get rid of floating precision errors
        _width = node["_width"].AsInt;
        _customData = node["_customData"];
    }

    public BeatmapObstacle(float time, int lineIndex, int type, float duration, int width, JSONNode customData = null) {
        _time = time;
        _lineIndex = lineIndex;
        _type = type;
        _duration = duration;
        _width = width;
        _customData = customData;
    }

    public override JSONNode ConvertToJSON() {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, 3);
        node["_lineIndex"] = _lineIndex;
        node["_type"] = _type;
        node["_duration"] = Math.Round(_duration, 3); //Get rid of float precision errors
        node["_width"] = _width;
        if (_customData != null) node["_customData"] = _customData;
        /*if (Settings.Instance.AdvancedShit) //This will be left commented unless its 100%, absolutely, positively required.
        {   
            //By request of Spooky Ghost to determine BeatWalls VS CM walls
            if (!node["_customData"].HasKey("_editor"))
            {
                node["_customData"]["_editor"] = BeatSaberSongContainer.Instance.song.editor;
            }
        }*/
        return node;
    }

    public override Type beatmapType { get; set; } = Type.OBSTACLE;
    public int _lineIndex;
    public int _type;
    public float _duration;
    public int _width;
    public uint id;

}