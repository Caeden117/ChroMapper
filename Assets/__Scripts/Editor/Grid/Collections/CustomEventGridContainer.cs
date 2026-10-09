using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Comparers;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomEventGridContainer : BeatmapObjectContainerCollection<BaseCustomEvent>,
                                        CMInput.ICustomEventsContainerActions
{
    public override IComparer<BaseCustomEvent> SortComparer => EventOrderComparer.Instance;

    [SerializeField] private GameObject customEventPrefab;
    [SerializeField] private TextMeshProUGUI customEventLabelPrefab;
    [SerializeField] private Transform customEventLabelTransform;
    [SerializeField] private GridChild gridChild;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CameraController playerCamera;
    private List<string> customEventTypes = new();
    public override ObjectType ContainerType => ObjectType.CustomEvent;

    public ReadOnlyCollection<string> CustomEventTypes => customEventTypes.AsReadOnly();

    public Dictionary<string, List<BaseCustomEvent>> EventsByTrack;
    private readonly List<FogAnimator> legacyFogAnimators = new();

    private void Start()
    {
        RefreshTrack();
        if (!Settings.Instance.AdvancedShit)
        {
            Debug.LogWarning("Disabling some objects since an Advanced setting is not enabled...");
            gridChild.Hide = true;
        }
    }

    public void LoadAll()
    {
        EventsByTrack = new Dictionary<string, List<BaseCustomEvent>>();
        // The camera rig survives map swaps, but player-track assignments belong to one map. Clear its
        // previous bindings before loading the new map's assignments.
        playerCamera.ClearPlayerTracks();

        var span = MapObjects.AsSpan();

        foreach (var ev in span) AddCustomEvent(ev);
        // Build the V2 fog assignment timeline after parsing all events so it retains callback times and file order.
        RebuildV2FogBinding();
    }

    public void RefreshBpmTiming(float jsonTime) => tracksManager.RefreshBpmTiming(jsonTime);

    public void OnAssignObjectstoTrack(InputAction.CallbackContext context)
    {
        if (EditContext.EditingMode.HasFlag(ViewableMode)
            && Settings.Instance.AdvancedShit
            && context.performed
            && !PersistentUI.Instance.InputBoxIsEnabled)
        {
            PersistentUI.Instance.ShowInputBox(
                "Assign the selected objects to a track ID.\n\n" + "If you dont know what you're doing, turn back now.",
                HandleTrackAssign);
        }
    }

    public void OnSetTrackFilter(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
            SetTrackFilter();
    }

    public void OnCreateNewEventType(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
            CreateNewType();
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        var customEvent = obj as BaseCustomEvent;
        if (!customEventTypes.Contains(customEvent.Type))
        {
            customEventTypes.Add(customEvent.Type);
            RefreshTrack();
        }

        AddCustomEvent(customEvent);
        if (Settings.Instance.MapVersion == 2) RebuildV2FogBinding();
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        var ev = obj as BaseCustomEvent;

        var tracks = ev.CustomTrack switch
        {
            JSONString s => new List<string> { s },
            JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
            _ => new List<string>()
        };

        foreach (var track in tracks)
        {
            EventsByTrack[track].Remove(ev);
            if (EventsByTrack[track].Count == 0)
            {
                EventsByTrack.Remove(track);
            }

            // Removing a V2 AnimateTrack event must stop both its fog properties and its object transforms.
            if (ev.Type == "AnimateTrack")
            {
                tracksManager.GetAnimationTrack(track).RemoveEvent(ev);
                if (Settings.Instance.MapVersion == 2 && HasLegacyFogProperty(ev))
                {
                    GetFogAnimator(track).RemoveEvent(ev);
                }
            }

            if (ev.Type == "AssignFogTrack" && Settings.Instance.MapVersion == 2)
                RebuildV2FogBinding();

            if (ev.Type == "AnimateComponent")
            {
                if (ev.Data?.HasKey("BloomFogEnvironment") == true)
                {
                    GetFogAnimator(track).RemoveEvent(ev);
                }

                if (ev.Data?.HasKey("TubeBloomPrePassLight") == true)
                {
                    GetTubeBloomAnimator(track).RemoveEvent(ev);
                }
            }
        }
    }

    private void AddCustomEvent(BaseCustomEvent ev)
    {
        var tracks = ev.CustomTrack switch
        {
            JSONString s => new List<string> { s },
            JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
            _ => new List<string>()
        };

        foreach (var track in tracks)
        {
            if (!EventsByTrack.ContainsKey(track))
            {
                EventsByTrack[track] = new List<BaseCustomEvent>();
            }

            EventsByTrack[track].Add(ev);

            if (ev.Type == "AnimateTrack")
            {
                var at = tracksManager.GetAnimationTrack(track);
                at.AddEvent(ev);

                // V2 fog parameters arrive through AnimateTrack. They can be parsed before AssignFogTrack
                // because the fog animator writes only while bound.
                if (Settings.Instance.MapVersion == 2 && HasLegacyFogProperty(ev))
                {
                    GetFogAnimator(track).AddLegacyEvent(ev);
                }
            }

            // AnimateTrack drives object transforms. Give each supported environment component its own
            // animator on the named track so AnimateComponent can drive its properties independently.
            if (ev.Type == "AnimateComponent")
            {
                if (ev.Data?.HasKey("BloomFogEnvironment") == true)
                {
                    GetFogAnimator(track).AddEvent(ev);
                }

                if (ev.Data?.HasKey("TubeBloomPrePassLight") == true)
                {
                    GetTubeBloomAnimator(track).AddEvent(ev);
                }
            }
        }

        switch (ev.Type)
        {
            case "AssignTrackParent":
                if (ev.DataParentTrack == null) return;
                var parent = tracksManager.GetAnimationTrack(ev.DataParentTrack);
                var children = ev.DataChildrenTracks switch
                {
                    JSONArray arr => arr,
                    JSONString s => JSONObject.Parse($"[{s}]").AsArray,
                    _ => new JSONArray(),
                };
                foreach (var tr in children)
                {
                    var at = tracksManager.GetAnimationTrack(tr.Value);
                    at.ParentWorldPositionStays = ev.DataWorldPositionStays ?? false;
                    at.Track.transform.SetParent(
                        parent.Track.ObjectParentTransform,
                        ev.DataWorldPositionStays ?? false);
                    if (at.Animator == null)
                    {
                        at.Animator = at.gameObject.AddComponent<ObjectAnimator>();
                        at.Animator.Context = BeatmapContext;
                    }

                    // Reinitialize a reused proxy animator to discard held transforms from the previous map.
                    // Use tracksManager.IsV2Map because Settings.MapVersion can still refer to the outgoing
                    // map during HardRefresh.
                    at.Animator.AttachToTrack(at.Track, tr.Value, tracksManager.IsV2Map);

                    // Parent already-attached environment targets under the child track's object parent so
                    // they follow the animated parent. Later attachments follow the same rule in
                    // ObjectAnimator.AttachToEnvironmentObject.
                    foreach (var child in at.Children)
                    {
                        child.ParentDirectTargetToTrack(at.Track.ObjectParentTransform, at.ParentWorldPositionStays);
                    }

                    if (!parent.Children.Contains(at.Animator))
                    {
                        parent.Children.Add(at.Animator);
                        at.Parents.Add(parent);
                        at.OnChildrenChanged();
                    }

                    // Rebind the property source even when the animator is already in parent.Children.
                    // Reparenting must update which track can reapply its held world position.
                    at.Animator.BindPropertySource(parent);
                }

                break;
            case "AssignPlayerToTrack":
                if (ev.CustomTrack == null) return;
                // The preview camera represents both Root and Head. Skip hand targets with a warning because
                // the editor has no VR controllers to animate.
                var targetKey = tracksManager.IsV2Map ? "_target" : "target";
                var playerTarget = ev.Data?.HasKey(targetKey) == true ? (string)ev.Data[targetKey] : "Root";
                if (playerTarget is not ("Root" or "Head"))
                {
                    Debug.LogWarning(
                        $"AssignPlayerToTrack target [{playerTarget}] has no editor preview representation; the event was skipped.");
                    return;
                }

                playerCamera.gameObject.SetActive(true);
                var track = tracksManager.GetAnimationTrack(ev.CustomTrack);
                playerCamera.AddPlayerTrack(ev.JsonTime, track);
                break;
        }
    }

    private FogAnimator GetFogAnimator(string track)
    {
        var fog = tracksManager.GetAnimationTrack(track).gameObject.GetOrAddComponent<FogAnimator>();
        if (fog.Atsc == null)
        {
            fog.Atsc = BeatmapContext.Atsc;
            fog.Context = BeatmapContext;
            BeatmapContext.Atsc.OnTimeChangedEarly += fog.PushOnStoppedTimeChanged;
        }

        return fog;
    }

    // A sorted assignment timeline selects the active V2 fog track at each seek. Enabling every track during
    // loading would make later assignments affect earlier times.
    private void RebuildV2FogBinding()
    {
        foreach (var animator in legacyFogAnimators) animator.SetLegacyBinding(null, false);
        legacyFogAnimators.Clear();
        if (Settings.Instance.MapVersion != 2) return;

        var binding = new LegacyFogBinding(BeatmapContext);
        FogAnimator controller = null;
        foreach (var ev in MapObjects)
        {
            if (ev.Type != "AssignFogTrack" || ev.CustomTrack is not JSONString track) continue;
            var animator = GetFogAnimator(track.Value);
            binding.Add(ev.JsonTime, animator);
            if (controller == null) controller = animator;
            if (!legacyFogAnimators.Contains(animator)) legacyFogAnimators.Add(animator);
        }

        if (controller == null) return;
        foreach (var animator in legacyFogAnimators)
            animator.SetLegacyBinding(binding, animator == controller);
    }

    private static bool HasLegacyFogProperty(BaseCustomEvent ev) =>
        ev.Data?.HasKey("_attenuation") == true
        || ev.Data?.HasKey("_offset") == true
        || ev.Data?.HasKey("_height") == true
        || ev.Data?.HasKey("_startY") == true;

    private TubeBloomAnimator GetTubeBloomAnimator(string track)
    {
        var tubeBloom = tracksManager.GetAnimationTrack(track).gameObject.GetOrAddComponent<TubeBloomAnimator>();
        if (tubeBloom.Atsc == null)
        {
            tubeBloom.Atsc = BeatmapContext.Atsc;
            BeatmapContext.Atsc.OnTimeChangedEarly += tubeBloom.PushOnStoppedTimeChanged;
        }

        return tubeBloom;
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    public override void RefreshPool(bool force)
    {
        if (UIMode.AnimationMode)
        {
            while (ObjectsWithContainers.Count > 0)
            {
                RecycleContainer(ObjectsWithContainers[ObjectsWithContainers.Count - 1], indexInObjectsWithContainers: ObjectsWithContainers.Count - 1); // Clearing list from index 0 made this O(N^2) including N^2/2 position shifts.... clearing from the back to front is O(N) if we dont scan with .Remove
            }
        }
        else
        {
            base.RefreshPool(force);
        }
    }

    private void RefreshTrack()
    {
        if (customEventTypes.Count == 0)
            gridChild.Hide = true;
        else
        {
            gridChild.Hide = false;
            gridChild.Lane = customEventTypes.Count;
        }

        for (var i = 0; i < customEventLabelTransform.childCount; i++)
            Destroy(customEventLabelTransform.GetChild(i).gameObject);
        foreach (var str in customEventTypes)
        {
            var newShit = Instantiate(customEventLabelPrefab.gameObject, customEventLabelTransform)
                .GetComponent<TextMeshProUGUI>();
            newShit.rectTransform.localPosition = new Vector3(customEventTypes.IndexOf(str), 0.25f, 0);
            newShit.text = str;
        }

        foreach (var obj in LoadedContainers.Values) obj.UpdateGridPosition();
    }

    internal override void SubscribeToCallbacks()
    {
        EditorScaleController.OnEditorScaleChanged += HandleEditorScaleChanged;
        LoadInitialMap.OnLevelLoaded += SetInitialTracks;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;
    }

    internal override void UnsubscribeToCallbacks()
    {
        EditorScaleController.OnEditorScaleChanged -= HandleEditorScaleChanged;
        LoadInitialMap.OnLevelLoaded -= SetInitialTracks;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;
    }

    private void SetInitialTracks()
    {
        var span = MapObjects.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            var customEvent = span[i];

            if (!customEventTypes.Contains(customEvent.Type))
            {
                customEventTypes.Add(customEvent.Type);
                RefreshTrack();
            }
        }
    }

    private void HandleEditorScaleChanged(float obj) => RefreshPool(true);

    private void CreateNewType()
    {
        if (PersistentUI.Instance.InputBoxIsEnabled) return;
        PersistentUI.Instance.ShowInputBox(
            "A new custom event type, I see?\n\n"
            + "Custom event types are for the advanced of advanced users. Node Editor and JSON knowledge are required for these babies.\n\n"
            + "If you dont know what these do, or don't have the documentation for them, turn back now.\n\n"
            + "But if you do, what would you like to name this new event type?",
            HandleNewTypeCreation,
            "NewCustomEventType");
    }

    private void HandleNewTypeCreation(string res)
    {
        if (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) return;
        customEventTypes.Add(res);
        customEventTypes = customEventTypes.OrderBy(x => x).ToList();
        RefreshTrack();
    }

    private void HandleTrackAssign(string res)
    {
        if (res is null) return;
        var modified = new List<BaseObject>();
        var value = (res == "")
            ? null
            : res;
        foreach (var obj in SelectionController.SelectedObjects)
        {
            var mod = BeatmapFactory.Clone(obj);
            modified.Add(mod);

            mod.CustomTrack = value;
            mod.WriteCustom();
        }

        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedCollectionAction(
                modified,
                SelectionController.SelectedObjects.ToList(),
                $"Assigned track to ({SelectionController.SelectedObjects.Count}) objects."),
            true);
    }

    public override ObjectContainer CreateContainer() =>
        CustomEventContainer.SpawnCustomEvent(null, this, ref customEventPrefab);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj) =>
        con.transform.localScale = Vector3.one * 0.75f;
}
