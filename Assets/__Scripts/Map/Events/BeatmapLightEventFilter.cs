using SimpleJSON;

public class BeatmapLightEventFilter: BeatmapObject
{
    public int FilterType; // f
    public int Partition; // p
    public int Section; // t
    public int Reverse; // r

    public BeatmapLightEventFilter(JSONNode node)
    {
        FilterType = RetrieveRequiredNode(node, "f").AsInt;
        Partition = RetrieveRequiredNode(node, "p").AsInt;
        Section = RetrieveRequiredNode(node, "t").AsInt;
        Reverse = RetrieveRequiredNode(node, "r").AsInt;
    }

    public BeatmapLightEventFilter() 
    {
        FilterType = 1;
        Partition = 1;
    }
    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        var node = new JSONObject();
        node["f"] = FilterType;
        node["p"] = Partition;
        node["t"] = Section;
        node["r"] = Reverse;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();

    public static bool SanityCheck(BeatmapLightEventFilter e)
    {
        if (e.FilterType == 1)
        {
            if (e.Section < 0)
            {
                PersistentUI.Instance.ShowDialogBox("Section must be positive.", null, PersistentUI.DialogBoxPresetType.Ok);
                return false;
            }
            if (e.Partition <= 0)
            {
                PersistentUI.Instance.ShowDialogBox("Partition must be positive.", null, PersistentUI.DialogBoxPresetType.Ok);
                return false;
            }
        }
        else if (e.FilterType == 2)
        {
            if (e.Section < 0)
            {
                PersistentUI.Instance.ShowDialogBox("Step must be non-negative.", null, PersistentUI.DialogBoxPresetType.Ok);
                return false;
            }
            if (e.Partition < 0)
            {
                PersistentUI.Instance.ShowDialogBox("Start index must be non-negative.", null, PersistentUI.DialogBoxPresetType.Ok);
                return false;
            }
        }
        return true;
    }
}
