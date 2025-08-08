public interface IMergeableAction
{
    ActionMergeType MergeType { get; set; }
    int MergeCount { get; set; }

    public IMergeableAction TryMerge(IMergeableAction previous);
    public bool CanMerge(IMergeableAction previous);
    public IMergeableAction DoMerge(IMergeableAction previous);
}
public enum ActionMergeType
{
    None,
    NoteDirectionChange,
    NotePreciseDirectionTweak,
    ArcHeadDirectionChange,
    ArcTailDirectionChange,
    ArcHeadMultTweak,
    ArcTailMultTweak,
    ChainSliceCountTweak,
    ChainSquishTweak,
    WallDurationTweak,
    WallLowerBoundTweak,
    WallUpperBoundTweak,
    EventMainTweak,
    EventAltTweak,
    BPMValueTweak,
    NJSValueTweak
}
