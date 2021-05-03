using SimpleJSON;
using System;
using UnityEngine;

[Serializable]
public class BeatmapObstacle : BeatmapObject, IBeatmapObjectBounds
{

    //These are uhh, assumptions...
    public const int VALUE_FULL_BARRIER = 0;
    public const int VALUE_HIGH_BARRIER = 1;

    public static readonly float MAPPINGEXTENSIONS_START_HEIGHT_MULTIPLIER = 1.35f;
    public static readonly float MAPPINGEXTENSIONS_UNITS_TO_FULL_HEIGHT_WALL = 1000 / 3.5f;

    /*
     * Obstacle Logic
     */

    public BeatmapObstacle(JSONNode node) {
        _time = RetrieveRequiredNode(node, "_time").AsFloat;
        _lineIndex = RetrieveRequiredNode(node, "_lineIndex").AsInt;
        _type = RetrieveRequiredNode(node, "_type").AsInt;
        _duration = RetrieveRequiredNode(node, "_duration").AsFloat;
        _width = RetrieveRequiredNode(node, "_width").AsInt;
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
        node["_time"] = Math.Round(_time, decimalPrecision);
        node["_lineIndex"] = _lineIndex;
        node["_type"] = _type;
        node["_duration"] = Math.Round(_duration, decimalPrecision); //Get rid of float precision errors
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

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion)
    {
        if (other is BeatmapObstacle obstacle)
        {
            if (IsNoodleExtensionsWall || obstacle.IsNoodleExtensionsWall)
            {
                return ConvertToJSON().ToString() == other.ConvertToJSON().ToString();
            }
            return _lineIndex == obstacle._lineIndex && _type == obstacle._type;
        }
        return false;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapObstacle obs)
        {
            _type = obs._type;
            _width = obs._width;
            _lineIndex = obs._lineIndex;
            _duration = obs._duration;
        }
    }

    public Vector2 GetCenter()
    {
        var bounds = GetShape();

        return new Vector2(bounds.Position + bounds.Width / 2, bounds.StartHeight + bounds.Height / 2);
    }

    public ObstacleBounds GetShape()
    {
        float position = _lineIndex - 2f; //Line index
        float startHeight = _type == VALUE_FULL_BARRIER ? 0 : 1.5f;
        float height = _type == VALUE_FULL_BARRIER ? 3.5f : 2;
        float width = _width;

        // ME

        if (_width >= 1000) width = ((float)_width - 1000) / 1000;
        if (_lineIndex >= 1000)
            position = (((float)_lineIndex - 1000) / 1000f) - 2f;
        else if (_lineIndex <= -1000)
            position = ((float)_lineIndex - 1000) / 1000f;

        if (_type > 1 && _type < 1000)
        {
            startHeight = _type / (750 / 3.5f); //start height 750 == standard wall height
            height = 3.5f;
        }
        else if (_type >= 1000 && _type <= 4000)
        {
            startHeight = 0; //start height = floor
            height = ((float)_type - 1000) / MAPPINGEXTENSIONS_UNITS_TO_FULL_HEIGHT_WALL; //1000 = no height, 2000 = full height
        }
        else if (_type > 4000)
        {
            float modifiedType = _type - 4001;
            startHeight = modifiedType % 1000 / MAPPINGEXTENSIONS_UNITS_TO_FULL_HEIGHT_WALL * MAPPINGEXTENSIONS_START_HEIGHT_MULTIPLIER;
            height = modifiedType / 1000 / MAPPINGEXTENSIONS_UNITS_TO_FULL_HEIGHT_WALL;
        }

        // NE

        //Just look at the difference in code complexity for Mapping Extensions support and Noodle Extensions support.
        //Hot damn.
        if (_customData != null)
        {
            if (_customData.HasKey("_position"))
            {
                Vector2 wallPos = _customData["_position"]?.ReadVector2() ?? Vector2.zero;
                position = wallPos.x;
                startHeight = wallPos.y;
            }
            if (_customData.HasKey("_scale"))
            {
                Vector2 wallSize = _customData["_scale"]?.ReadVector2() ?? Vector2.one;
                width = wallSize.x;
                height = wallSize.y;
            }
        }

        return new ObstacleBounds(width, height, position, startHeight);
    }

    public bool IsNoodleExtensionsWall => _customData != null &&
        (_customData.HasKey("_position") || _customData.HasKey("_scale")
            || _customData.HasKey("_localRotation") || _customData.HasKey("_rotation"));
    public override Type beatmapType { get; set; } = Type.OBSTACLE;
    public int _lineIndex;
    public int _type;
    public float _duration;
    public int _width;
}
