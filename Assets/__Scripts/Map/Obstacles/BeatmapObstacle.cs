using System;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class BeatmapObstacle : BeatmapObject, IBeatmapObjectBounds
{
    //These are uhh, assumptions...
    public const int ValueFullBarrier = 0;
    public const int ValueHighBarrier = 1;

    public static readonly float MappingextensionsStartHeightMultiplier = 1.35f;
    public static readonly float MappingextensionsUnitsToFullHeightWall = 1000 / 3.5f;
    [FormerlySerializedAs("_lineIndex")] public int LineIndex;
    [FormerlySerializedAs("_type")] public int Type;
    [FormerlySerializedAs("_duration")] public float Duration;
    [FormerlySerializedAs("_width")] public int Width;

    /*
     * Obstacle Logic
     */
    protected BeatmapObstacle() { }

    public BeatmapObstacle(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "_time").AsFloat;
        LineIndex = RetrieveRequiredNode(node, "_lineIndex").AsInt;
        Type = RetrieveRequiredNode(node, "_type").AsInt;
        Duration = RetrieveRequiredNode(node, "_duration").AsFloat;
        Width = RetrieveRequiredNode(node, "_width").AsInt;
        CustomData = node["_customData"];
    }

    public BeatmapObstacle(float time, int lineIndex, int type, float duration, int width, JSONNode customData = null)
    {
        Time = time;
        LineIndex = lineIndex;
        Type = type;
        Duration = duration;
        Width = width;
        CustomData = customData;
    }

    public bool IsNoodleExtensionsWall => CustomData != null &&
                                          (CustomData.HasKey("_position") || CustomData.HasKey("_scale")
                                                                           || CustomData.HasKey("_localRotation") ||
                                                                           CustomData.HasKey("_rotation"));

    public override ObjectType BeatmapType { get; set; } = ObjectType.Obstacle;

    public Vector2 GetCenter()
    {
        var bounds = GetShape();

        return new Vector2(bounds.Position + (bounds.Width / 2), bounds.StartHeight + (bounds.Height / 2));
    }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(Time, DecimalPrecision);
        node["_lineIndex"] = LineIndex;
        node["_type"] = Type;
        node["_duration"] = Math.Round(Duration, DecimalPrecision); //Get rid of float precision errors
        node["_width"] = Width;
        if (CustomData != null) node["_customData"] = CustomData;
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
                return ConvertToJson().ToString() == other.ConvertToJson().ToString();
            return LineIndex == obstacle.LineIndex && Type == obstacle.Type;
        }

        return false;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapObstacle obs)
        {
            Type = obs.Type;
            Width = obs.Width;
            LineIndex = obs.LineIndex;
            Duration = obs.Duration;
        }
    }

    public ObstacleBounds GetShape()
    {
        var position = LineIndex - 2f; //Line index
        var startHeight = Type == ValueFullBarrier ? 0 : 1.5f;
        var height = Type == ValueFullBarrier ? 3.5f : 2;
        float width = Width;

        // ME

        if (Width >= 1000) width = ((float)Width - 1000) / 1000;
        if (LineIndex >= 1000)
            position = (((float)LineIndex - 1000) / 1000f) - 2f;
        else if (LineIndex <= -1000)
            position = ((float)LineIndex - 1000) / 1000f;

        if (Type > 1 && Type < 1000)
        {
            startHeight = Type / (750 / 3.5f); //start height 750 == standard wall height
            height = 3.5f;
        }
        else if (Type >= 1000 && Type <= 4000)
        {
            startHeight = 0; //start height = floor
            height = ((float)Type - 1000) /
                     MappingextensionsUnitsToFullHeightWall; //1000 = no height, 2000 = full height
        }
        else if (Type > 4000)
        {
            float modifiedType = Type - 4001;
            startHeight = modifiedType % 1000 / MappingextensionsUnitsToFullHeightWall *
                          MappingextensionsStartHeightMultiplier;
            height = modifiedType / 1000 / MappingextensionsUnitsToFullHeightWall;
        }

        // NE

        //Just look at the difference in code complexity for Mapping Extensions support and Noodle Extensions support.
        //Hot damn.
        if (CustomData != null)
        {
            if (CustomData.HasKey("_position"))
            {
                var wallPos = CustomData["_position"]?.ReadVector2() ?? Vector2.zero;
                position = wallPos.x;
                startHeight = wallPos.y;
            }

            if (CustomData.HasKey("_scale"))
            {
                var wallSize = CustomData["_scale"]?.ReadVector2() ?? Vector2.one;
                width = wallSize.x;
                height = wallSize.y;
            }
        }

        return new ObstacleBounds(width, height, position, startHeight);
    }
}
