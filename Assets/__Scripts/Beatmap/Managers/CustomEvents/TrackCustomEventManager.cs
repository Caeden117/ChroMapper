using System.Collections.Generic;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base.Customs;
using SimpleJSON;
using UnityEngine;

/// <summary>Owns track event registration and reversible parent assignments, independently of grid visuals.</summary>
public sealed class TrackCustomEventManager
{
    private readonly TracksManager tracksManager;
    private readonly BeatmapRuntimeContext context;
    private readonly CameraController playerCamera;
    private readonly Dictionary<TrackAnimator, ParentState> parentStates = new();
    private readonly Dictionary<TrackAnimator, TrackAnimator> parents = new();
    private TrackAnimator[] sampledTracks = System.Array.Empty<TrackAnimator>();
    private ObjectAnimator[] sampledChildren = System.Array.Empty<ObjectAnimator>();
    private int sampledRevision = -1;

    public TrackCustomEventManager(TracksManager tracksManager, BeatmapRuntimeContext context, CameraController playerCamera)
    {
        this.tracksManager = tracksManager;
        this.context = context;
        this.playerCamera = playerCamera;
    }

    public void Rebuild(IEnumerable<BaseCustomEvent> events)
    {
        ResetParenting();
        if (playerCamera != null) playerCamera.ClearPlayerTracks();
        tracksManager.CustomEventsByTrack.Clear();
        tracksManager.PathAnimations.Reset();
        foreach (var track in tracksManager.AnimationTracks) track.ResetEvents(false);

        foreach (var evt in events)
        {
            foreach (var name in TrackNames(evt.CustomTrack))
            {
                if (!tracksManager.CustomEventsByTrack.TryGetValue(name, out var list))
                    tracksManager.CustomEventsByTrack.Add(name, list = new List<BaseCustomEvent>());
                list.Add(evt);
                if (evt.Type == "AnimateTrack") tracksManager.GetAnimationTrack(name).AddEvent(evt, false);
                else if (evt.Type == "AssignPathAnimation") tracksManager.PathAnimations.Add(name, evt);
            }

            RegisterPlayerEvent(evt);
        }

        // The event manager refreshes each target once after all of its tracks have been rebuilt.
        foreach (var track in tracksManager.AnimationTracks.ToArray()) track.RefreshProperties(false);
    }

    /// <summary>Rebuilds changed track channels and path sources while retaining unrelated registrations.</summary>
    /// <param name="orderedEvents">The complete current event list in runtime dispatch order.</param>
    /// <param name="affectedTracks">Tracks whose AnimateTrack channels changed, including old track names.</param>
    /// <param name="affectedPathTracks">Tracks whose AssignPathAnimation sources changed, including old track names.</param>
    /// <param name="affectedObjects">Receives affected track users and their current parenting descendants.</param>
    /// <param name="affectedPathObjects">Receives direct path users that need to rebind their object-specific definitions.</param>
    /// <param name="affectedEventTracks">Additional track names whose editor event index changed.</param>
    /// <param name="affectedPoseTracks">Optional subset whose transform curves changed; other edits retain parenting.</param>
    /// <returns>True when changed channels can alter poses captured by parent assignments and require structural replay.</returns>
    public bool RefreshTracks(IReadOnlyList<BaseCustomEvent> orderedEvents, ISet<string> affectedTracks,
        ISet<string> affectedPathTracks, HashSet<ObjectAnimator> affectedObjects,
        HashSet<ObjectAnimator> affectedPathObjects, ISet<string> affectedEventTracks = null,
        ISet<string> affectedPoseTracks = null)
    {
        var indexedNames = new HashSet<string>(affectedTracks);
        indexedNames.UnionWith(affectedPathTracks);
        if (affectedEventTracks != null) indexedNames.UnionWith(affectedEventTracks);
        if (indexedNames.Count == 0) return false;
        var eventsByTrack = new Dictionary<string, List<BaseCustomEvent>>();
        foreach (var name in indexedNames) eventsByTrack.Add(name, new List<BaseCustomEvent>());
        foreach (var evt in orderedEvents)
            foreach (var name in TrackNames(evt.CustomTrack))
                if (eventsByTrack.TryGetValue(name, out var events)) events.Add(evt);

        foreach (var (name, events) in eventsByTrack)
        {
            if (events.Count == 0) tracksManager.CustomEventsByTrack.Remove(name);
            else tracksManager.CustomEventsByTrack[name] = events;
        }

        var affectedAnimationTracks = new Dictionary<string, TrackAnimator>();
        foreach (var track in tracksManager.AnimationTracks)
            if (affectedTracks.Contains(track.name) || affectedPathTracks.Contains(track.name))
                affectedAnimationTracks.Add(track.name, track);

        foreach (var name in affectedTracks)
        {
            var events = eventsByTrack[name];
            if (!affectedAnimationTracks.TryGetValue(name, out var track))
            {
                if (!events.Any(evt => evt.Type == "AnimateTrack")) continue;
                track = tracksManager.GetAnimationTrack(name);
                affectedAnimationTracks.Add(name, track);
            }
            track.RebuildEvents(events);
        }

        foreach (var name in affectedPathTracks)
        {
            tracksManager.PathAnimations.Replace(name, eventsByTrack[name].Where(evt => evt.Type == "AssignPathAnimation"));
            if (!affectedAnimationTracks.TryGetValue(name, out var track)) continue;
            foreach (var child in track.Children)
            {
                if (child == null || child.TargetType is not (ObjectAnimator.TargetTypes.GameplayObject or ObjectAnimator.TargetTypes.Material)) continue;
                affectedObjects.Add(child);
                affectedPathObjects.Add(child);
            }
        }

        CollectAffectedTrackObjects(affectedTracks, affectedAnimationTracks, affectedObjects);
        return RequiresParentReplay(orderedEvents, affectedPoseTracks ?? affectedTracks);
    }

    /// <summary>Replaces player assignments in dispatch order without rebuilding track animation channels.</summary>
    public void RefreshPlayerTracks(IReadOnlyList<BaseCustomEvent> orderedEvents)
    {
        if (playerCamera == null) return;
        playerCamera.ClearPlayerTracks();
        foreach (var evt in orderedEvents) RegisterPlayerEvent(evt);
    }

    private void RegisterPlayerEvent(BaseCustomEvent evt)
    {
        if (playerCamera == null || evt.Type != "AssignPlayerToTrack" || evt.CustomTrack is not JSONString nameNode) return;
        var targetKey = BeatSaberSongContainer.Instance.Map.MajorVersion == 2 ? "_target" : "target";
        var target = evt.Data[targetKey];
        if (target != null && target.Value is not ("Root" or "Head"))
        {
            Debug.LogWarning($"AssignPlayerToTrack target [{target.Value}] has no editor preview representation; the event was skipped.");
            return;
        }
        playerCamera.gameObject.SetActive(true);
        playerCamera.AddPlayerTrack(evt.JsonTime, tracksManager.GetAnimationTrack(nameNode));
    }

    private void CollectAffectedTrackObjects(ISet<string> names,
        Dictionary<string, TrackAnimator> affectedTracks, HashSet<ObjectAnimator> affectedObjects)
    {
        if (names.Count == 0) return;
        var childrenByParent = new Dictionary<TrackAnimator, List<TrackAnimator>>();
        foreach (var (child, parent) in parents)
        {
            if (!childrenByParent.TryGetValue(parent, out var children))
                childrenByParent.Add(parent, children = new List<TrackAnimator>());
            children.Add(child);
        }

        var pending = new Queue<TrackAnimator>();
        var visited = new HashSet<TrackAnimator>();
        foreach (var name in names)
            if (affectedTracks.TryGetValue(name, out var track)) pending.Enqueue(track);
        while (pending.Count > 0)
        {
            var track = pending.Dequeue();
            if (track == null || !visited.Add(track)) continue;
            foreach (var child in track.Children)
                if (child != null) affectedObjects.Add(child);
            if (!childrenByParent.TryGetValue(track, out var children)) continue;
            foreach (var child in children) pending.Enqueue(child);
        }
    }

    private bool RequiresParentReplay(IReadOnlyList<BaseCustomEvent> orderedEvents, ISet<string> names)
    {
        if (names.Count == 0) return false;
        // An ancestor must itself occur in a parent assignment, so the complete event list
        // covers dependencies that are no longer visible in the current parent hierarchy.
        foreach (var evt in orderedEvents)
        {
            if (evt.Type != "AssignTrackParent" || evt.DataParentTrack is not JSONString parent
                || evt.SongBpmTime > context.Atsc.CurrentSongBpmTime) continue;
            if (names.Contains(parent.Value)) return true;
            foreach (var child in TrackNames(evt.DataChildrenTracks))
                if (names.Contains(child)) return true;
        }
        return false;
    }

    public void AssignParent(BaseCustomEvent evt)
    {
        if (evt.DataParentTrack is not JSONString parentName) return;
        var parent = tracksManager.GetAnimationTrack(parentName);
        Sample(evt.JsonTime, true);
        foreach (var name in TrackNames(evt.DataChildrenTracks))
        {
            var child = tracksManager.GetAnimationTrack(name);
            if (child == parent || parent.transform.IsChildOf(child.transform))
            {
                Debug.LogWarning($"Skipping cyclic track parent assignment at beat {evt.JsonTime}.");
                continue;
            }

            if (!parentStates.ContainsKey(child)) parentStates.Add(child, new ParentState(child.Track));
            DetachParent(child);
            child.Track.transform.SetParent(parent.Track.ObjectParentTransform, evt.DataWorldPositionStays ?? false);
            if (child.Animator == null) child.Animator = child.gameObject.AddComponent<ObjectAnimator>();
            child.Animator.Context = context;
            child.Animator.TracksManager = tracksManager;
            child.Animator.AttachToTrack(child.Track, name, tracksManager.IsV2Map);
            child.ParentWorldPositionStays = evt.DataWorldPositionStays ?? false;
            parents.Add(child, parent);
            child.Parents.Add(parent);
            parent.AddChild(child.Animator);
            child.Animator.BindPropertySource(parent);
            foreach (var target in child.Children.ToArray())
                if (target != null)
                    target.ParentDirectTargetToTrack(child.Track.ObjectParentTransform, child.ParentWorldPositionStays);
            child.OnChildrenChanged();
        }
    }

    public void ResetParenting()
    {
        foreach (var child in parents.Keys.ToArray()) DetachParent(child);
        foreach (var (child, state) in parentStates)
        {
            if (child == null) continue;
            if (child.Animator != null)
            {
                child.Animator.ResetData();
                child.Animator.enabled = false;
            }
            state.Restore(child.Track);
        }
        parentStates.Clear();
    }

    private void DetachParent(TrackAnimator child)
    {
        if (!parents.Remove(child, out var parent)) return;
        foreach (var target in child.Children.ToArray())
            if (target != null) target.ClearDirectTrackParent();
        child.ParentWorldPositionStays = false;
        if (parent != null && child.Animator != null) parent.RemoveChild(child.Animator);
        child.Parents.Remove(parent);
    }

    public void Sample(float jsonTime, bool transformsOnly = false)
    {
        if (!transformsOnly && playerCamera != null) playerCamera.RefreshPlayerTrack(jsonTime);
        if (sampledRevision != tracksManager.AnimationRevision)
        {
            sampledTracks = tracksManager.AnimationTracks.ToArray();
            sampledChildren = sampledTracks.SelectMany(track => track.CachedChildren)
                .Where(child => child != null).Distinct().OrderBy(child => TargetDepth(child.LocalTarget)).ToArray();
            sampledRevision = tracksManager.AnimationRevision;
        }
        foreach (var child in sampledChildren)
            if (child != null && child.isActiveAndEnabled) child.ClearAnimationContributions();
        foreach (var track in sampledTracks)
            if (track != null) track.SampleAt(jsonTime);
        var songBpmTime = BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime) ?? jsonTime;
        foreach (var child in sampledChildren)
        {
            if (child == null || !child.isActiveAndEnabled) continue;
            if (transformsOnly && child.TargetType != ObjectAnimator.TargetTypes.Transform) continue;
            child.ApplyCustomEventSample(transformsOnly, songBpmTime);
        }
    }

    public void Reset()
    {
        ResetParenting();
        if (playerCamera != null) playerCamera.ClearPlayerTracks();
        if (tracksManager == null) return;
        tracksManager.CustomEventsByTrack.Clear();
        tracksManager.PathAnimations.Reset();
        foreach (var track in tracksManager.AnimationTracks.ToArray())
            if (track != null) track.ResetEvents();
    }

    private static int TargetDepth(Transform target)
    {
        var depth = 0;
        while (target != null)
        {
            depth++;
            target = target.parent;
        }
        return depth;
    }

    private static IEnumerable<string> TrackNames(JSONNode node)
    {
        if (node is JSONString name) yield return name;
        else if (node is JSONArray names)
            foreach (var child in names.Children)
                if (child is JSONString item) yield return item;
    }

    private readonly struct ParentState
    {
        private readonly Transform parent;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly Vector3 scale;
        private readonly Vector3 innerPosition;
        private readonly Quaternion innerRotation;
        private readonly Vector3 innerScale;

        public ParentState(Track track)
        {
            parent = track.transform.parent;
            position = track.transform.localPosition;
            rotation = track.transform.localRotation;
            scale = track.transform.localScale;
            innerPosition = track.ObjectParentTransform.localPosition;
            innerRotation = track.ObjectParentTransform.localRotation;
            innerScale = track.ObjectParentTransform.localScale;
        }

        public void Restore(Track track)
        {
            track.transform.SetParent(parent, false);
            track.transform.SetLocalPositionAndRotation(position, rotation);
            track.transform.localScale = scale;
            track.ObjectParentTransform.SetLocalPositionAndRotation(innerPosition, innerRotation);
            track.ObjectParentTransform.localScale = innerScale;
        }
    }
}
