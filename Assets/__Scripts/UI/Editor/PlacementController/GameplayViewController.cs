using SimpleJSON;
using UnityEngine;

public class GameplayViewController : MonoBehaviour, IEditorStateProvider
{
    // Keep note-placement properties in their own metadata schema instead of resetting to Settings defaults on load.
    public string StateKey => "gameplayPlacement";

    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private ArcPlacement arcPlacement;
    [SerializeField] private ChainPlacement chainPlacement;
    [SerializeField] private PlacementLaneController laneController;

    [SerializeField] private TextBoxFloatComponent arcHeadMultiplierInput;
    [SerializeField] private TextBoxFloatComponent arcTailMultiplierInput;
    [SerializeField] private TextBoxFloatComponent chainSquishInput;
    [SerializeField] private TextBoxIntComponent chainCountInput;

    [SerializeField] private TextBoxIntComponent laneCountInput;
    [SerializeField] private TextBoxIntComponent wallExtendInput;

    private void Start()
    {
        arcHeadMultiplierInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentMultiplierPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollArcMultiplier)
            .OnEndEdit(HandleArcHeadMultiplierChanged)
            .OnValueChanged(HandleArcHeadMultiplierChanged)
            .SetValueWithoutNotify(Settings.Instance.DefaultArcHeadMultiplier);
        arcTailMultiplierInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentMultiplierPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollArcMultiplier)
            .OnEndEdit(HandleArcTailMultiplierChanged)
            .OnValueChanged(HandleArcTailMultiplierChanged)
            .SetValueWithoutNotify(Settings.Instance.DefaultArcTailMultiplier);
        chainSquishInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentMultiplierPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollChainSquish)
            .OnEndEdit(HandleChainSquishChanged)
            .OnValueChanged(HandleChainSquishChanged)
            .SetValueWithoutNotify(Settings.Instance.DefaultChainSquish);
        chainCountInput
            .OnEndEdit(HandleChainCountChanged)
            .OnValueChanged(HandleChainCountChanged)
            .SetValueWithoutNotify(Settings.Instance.DefaultChainSliceCount);

        laneCountInput
            .WithInvertScroll(() => Settings.Instance.InvertScrollChainSegmentCount)
            .OnEndEdit(HandleLaneCountChanged)
            .OnValueChanged(HandleLaneCountChanged)
            .SetValueWithoutNotify(laneController.LaneCount);
        wallExtendInput
            .OnEndEdit(HandleWallExtendChanged)
            .OnValueChanged(HandleWallExtendChanged)
            .SetValueWithoutNotify(laneController.ObstacleLaneExtend);

        EditorStateService.Register(this);
    }

    // Unregister only when this controller is destroyed so an inactive placement tab remains saveable.
    private void OnDestroy() => EditorStateService.Unregister(this);

    // Let this owner populate its own metadata node during map save or autosave.
    public void CaptureEditorState(JSONObject data)
    {
        data["arcHeadMultiplier"] = arcPlacement.HeadMultiplier;
        data["arcTailMultiplier"] = arcPlacement.TailMultiplier;
        data["chainSquish"] = chainPlacement.Squish;
        data["chainSliceCount"] = chainPlacement.SliceCount;
        data["laneCount"] = laneController.LaneCount;
        data["obstacleLaneExtend"] = laneController.ObstacleLaneExtend;
    }

    // Restore both the placement owners and their rendered inputs without invoking user-edit callbacks.
    public void RestoreEditorState(JSONNode data)
    {
        if (!data.IsObject)
        {
            return;
        }

        if (data.HasKey("arcHeadMultiplier"))
        {
            var value = data["arcHeadMultiplier"].AsFloat;
            arcPlacement.HeadMultiplier = value;
            arcHeadMultiplierInput.SetValueWithoutNotify(value);
        }

        if (data.HasKey("arcTailMultiplier"))
        {
            var value = data["arcTailMultiplier"].AsFloat;
            arcPlacement.TailMultiplier = value;
            arcTailMultiplierInput.SetValueWithoutNotify(value);
        }

        if (data.HasKey("chainSquish"))
        {
            var value = data["chainSquish"].AsFloat;
            chainPlacement.Squish = value;
            chainSquishInput.SetValueWithoutNotify(value);
        }

        if (data.HasKey("chainSliceCount"))
        {
            var value = data["chainSliceCount"].AsInt;
            chainPlacement.SliceCount = value;
            chainCountInput.SetValueWithoutNotify(value);
        }

        if (data.HasKey("laneCount"))
        {
            var value = data["laneCount"].AsInt;
            laneController.LaneCount = value;
            laneCountInput.SetValueWithoutNotify(value);
        }

        if (data.HasKey("obstacleLaneExtend"))
        {
            var value = data["obstacleLaneExtend"].AsInt;
            laneController.ObstacleLaneExtend = value;
            wallExtendInput.SetValueWithoutNotify(value);
        }
    }

    // Receive this owner's cached node when map metadata finishes loading after Start.
    public void LoadEditorState(JSONNode data) => RestoreEditorState(data);

    private void HandleArcHeadMultiplierChanged(float value) => arcPlacement.HeadMultiplier = value;
    private void HandleArcTailMultiplierChanged(float value) => arcPlacement.TailMultiplier = value;
    private void HandleChainSquishChanged(float value) => chainPlacement.Squish = value;
    private void HandleChainCountChanged(int value) => chainPlacement.SliceCount = value;
    private void HandleLaneCountChanged(int value) => laneController.LaneCount = value;
    private void HandleWallExtendChanged(int value) => laneController.ObstacleLaneExtend = value;
}
