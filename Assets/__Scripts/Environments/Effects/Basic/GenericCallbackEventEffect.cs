using System;
using Beatmap.Base;

public class GenericCallbackEventEffect : BasicEventEffect<BasicEventStateData>,
                                          IEffectStateSignal<(int index, BasicEventStateData state)>
{
    public event Action<(int index, BasicEventStateData state)> OnStateChanged;

    private readonly BasicEventStateChunksContainer<BasicEventStateData> container = new();

    public override void Initialize() => InitializeStates(container);

    public override void Refresh() => UpdateObject(container.CurrentState);

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, isPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(BasicEventStateData state)
    {
        var index = container.Collection.IndexOf(state);
        OnStateChanged?.Invoke((index, state));
    }

    public (int index, BasicEventStateData state) GetCurrentState() =>
        container?.CurrentState == null
            ? (-1, CreateState(new()))
            : (container.Collection.IndexOf(container.CurrentState), container.CurrentState);

    protected override BasicEventStateData CreateState(BaseEvent data) => new(data);

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        HandleInsertState(container, state);
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var state = HandleRemoveState(container, reference, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(reference.SongBpmTime);
        UpdateObject(container.CurrentState);
    }
}
