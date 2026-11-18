using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Graphics/VisualSettings")]
// TODO: expand this to be proper model selection
public class VisualSettingsSO : ScriptableObject
{
    public VisualRepositorySO Repository;

    [Header("Default Models")] public VisualModelSO DefaultBlock;
    public VisualModelSO DefaultEvent;
    public VisualModelSO DefaultNote;
    public VisualModelSO DefaultBomb;
    public VisualModelSO DefaultChainHead;
    public VisualModelSO DefaultChainLink;

    // Black Out's capture started with 7,500 loaded Basic Event nodes and created another 5,100. Reserve some headroom.
    private readonly FastAction blockModelChanged = new(16384);
    private readonly FastAction eventModelChanged = new(16384);

    // These are estimates for dense note and chain maps. Each NoteContainer subscribes to all three note models.
    private readonly FastAction noteModelChanged = new(2048);
    private readonly FastAction bombModelChanged = new(2048);
    private readonly FastAction chainHeadModelChanged = new(2048);
    private readonly FastAction chainLinkModelChanged = new(2048);

    public event Action OnBlockModelChanged
    {
        add => blockModelChanged.Add(value);
        remove => blockModelChanged.Remove(value);
    }

    public event Action OnEventModelChanged
    {
        add => eventModelChanged.Add(value);
        remove => eventModelChanged.Remove(value);
    }

    public event Action OnNoteModelChanged
    {
        add => noteModelChanged.Add(value);
        remove => noteModelChanged.Remove(value);
    }

    public event Action OnBombModelChanged
    {
        add => bombModelChanged.Add(value);
        remove => bombModelChanged.Remove(value);
    }

    public event Action OnChainHeadModelChanged
    {
        add => chainHeadModelChanged.Add(value);
        remove => chainHeadModelChanged.Remove(value);
    }

    public event Action OnChainLinkModelChanged
    {
        add => chainLinkModelChanged.Add(value);
        remove => chainLinkModelChanged.Remove(value);
    }

    public void OnEnable()
    {
    #if UNITY_EDITOR
        if (!Application.isPlaying) return;
    #endif
        Settings.NotifyBySettingName("EventModels", HandleBlockModelChanged);
        Settings.NotifyBySettingName("EventModels", HandleEventModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleNoteModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleBombModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleChainHeadModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleChainLinkModelChanged);
        Repository.NoteModelChanged += HandleRepositoryNoteModelChanged;
        CacheEventModelName();
    }

    public void OnDisable()
    {
    #if UNITY_EDITOR
        if (!Application.isPlaying) return;
    #endif
        Settings.StopNotifyingBySettingName("EventModels", HandleBlockModelChanged);
        Settings.StopNotifyingBySettingName("EventModels", HandleEventModelChanged);
        Settings.StopNotifyingBySettingName("NoteModels", HandleNoteModelChanged);
        Settings.StopNotifyingBySettingName("NoteModels", HandleBombModelChanged);
        Settings.StopNotifyingBySettingName("NoteModels", HandleChainHeadModelChanged);
        Settings.StopNotifyingBySettingName("NoteModels", HandleChainLinkModelChanged);
        Repository.NoteModelChanged -= HandleRepositoryNoteModelChanged;
    }

    private string cachedEventModelName;

    private void CacheEventModelName() =>
        cachedEventModelName = "CM_Event_" + Settings.Instance.EventModels.Replace(' ', '_');

    private void HandleBlockModelChanged(object _) => blockModelChanged.Call();

    private void HandleEventModelChanged(object _)
    {
        CacheEventModelName();
        eventModelChanged.Call();
    }

    private void HandleNoteModelChanged(object _) => noteModelChanged.Call();
    private void HandleBombModelChanged(object _) => bombModelChanged.Call();
    private void HandleChainHeadModelChanged(object _) => chainHeadModelChanged.Call();
    private void HandleChainLinkModelChanged(object _) => chainLinkModelChanged.Call();

    private void HandleRepositoryNoteModelChanged()
    {
        noteModelChanged.Call();
        bombModelChanged.Call();
        chainHeadModelChanged.Call();
        chainLinkModelChanged.Call();
    }

    public VisualModelSO GetBlockModel() => Repository.ModelsByName.GetValueOrDefault("CM_Block", DefaultBlock);

    public VisualModelSO GetEventBlockModel() =>
        Repository.ModelsByName.GetValueOrDefault("CM_Event_Block", DefaultBlock);

    public VisualModelSO GetEventModel() =>
        Repository.ModelsByName.GetValueOrDefault(
            cachedEventModelName,
            DefaultEvent);

    public VisualModelSO GetNoteModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteLeft
            : DefaultNote;

    public VisualModelSO GetNoteLeftModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteLeft
            : DefaultNote;

    public VisualModelSO GetNoteRightModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteRight
            : DefaultNote;

    public VisualModelSO GetNoteDotLeftModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteDotLeft
            : DefaultNote;

    public VisualModelSO GetNoteDotRightModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteDotRight
            : DefaultNote;

    public VisualModelSO GetBombModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteBomb
            : DefaultBomb;

    public VisualModelSO GetBurstSliderLeftModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderLeft
            : DefaultChainLink;

    public VisualModelSO GetBurstSliderRightModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderRight
            : DefaultChainLink;

    public VisualModelSO GetBurstSliderHeadLeftModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderHeadLeft
            : DefaultChainHead;

    public VisualModelSO GetBurstSliderHeadRightModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderHeadRight
            : DefaultChainHead;

    public VisualModelSO GetBurstSliderHeadDotLeftModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderHeadDotLeft
            : DefaultChainHead;

    public VisualModelSO GetBurstSliderHeadDotRightModel() =>
        Repository.TryGetNoteModel(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderHeadDotRight
            : DefaultChainHead;
}
