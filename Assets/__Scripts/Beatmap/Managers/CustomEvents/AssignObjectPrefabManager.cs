using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class AssignObjectPrefabManager
{
    public VisualRepositorySO VisualRepository;
    public TracksManager TracksManager;

    public struct ActiveModelResult
    {
        /// <summary>A null override selects the object's default model when HasOverride is true.</summary>
        public VisualModelSO OverrideModel;
        /// <summary>Additional models; a null entry selects the object's default model.</summary>
        public readonly List<VisualModelSO> AdditiveModels;
        public bool HasOverride;

        public ActiveModelResult(List<VisualModelSO> list)
        {
            OverrideModel = null;
            AdditiveModels = list;
            HasOverride = false;
        }
    }

    private readonly Dictionary<string, TrackModelState> trackToTrackModelState = new();

    private TrackModelState.ModelContainer.PriorityModel cachedSet;
    private readonly List<TrackModelState.ModelContainer.PriorityModel> cachedAdditive = new();
    private ActiveModelResult cachedResult = new(new());

    public ref ActiveModelResult GetCurrentModels(TrackModelState.Kind kind, string track)
    {
        cachedResult.OverrideModel = null;
        cachedResult.HasOverride = false;
        cachedResult.AdditiveModels.Clear();

        if (!trackToTrackModelState.TryGetValue(track, out var trackModel)) return ref cachedResult;
        var model = trackModel.GetModel(kind);
        if (model.HasSet)
        {
            cachedResult.OverrideModel = model.Model.Model;
            cachedResult.HasOverride = true;
        }

        foreach (var additive in model.Models) AddResolvedModel(additive.Model);

        return ref cachedResult;
    }

    public ref ActiveModelResult GetCurrentModels(TrackModelState.Kind kind, string[] tracks)
    {
        cachedResult.OverrideModel = null;
        cachedResult.HasOverride = false;
        cachedResult.AdditiveModels.Clear();
        cachedSet.Priority = -1;
        cachedAdditive.Clear();

        foreach (var track in tracks)
        {
            if (!trackToTrackModelState.TryGetValue(track, out var trackModel)) continue;
            var model = trackModel.GetModel(kind);
            if (model.HasSet && model.Model.Priority >= cachedSet.Priority)
            {
                cachedSet = model.Model;
                cachedResult.HasOverride = true;
            }

            cachedAdditive.AddRange(model.Models);
        }

        if (cachedResult.HasOverride) cachedResult.OverrideModel = cachedSet.Model;
        foreach (var additive in cachedAdditive)
        {
            if (!cachedResult.HasOverride || additive.Priority >= cachedSet.Priority)
                AddResolvedModel(additive.Model);
        }

        return ref cachedResult;
    }

    private void AddResolvedModel(VisualModelSO model)
    {
        // A null assignment represents the editor's default model.
        if ((!cachedResult.HasOverride || cachedResult.OverrideModel == null) && model == null) return;
        if (cachedResult.HasOverride && cachedResult.OverrideModel == model) return;
        if (!cachedResult.AdditiveModels.Contains(model)) cachedResult.AdditiveModels.Add(model);
    }

    public void Assign(CustomEventStateData state, int index) => Apply(state, index);

    private void Apply(CustomEventStateData state, int index)
    {
        var data = state.Base.Data;
        var additive = data["loadMode"] == "Additive";
        ApplyGroup(data["colorNotes"], TrackModelState.Kind.DirectionalNote, index, additive);
        ApplyGroup(data["bombNotes"], TrackModelState.Kind.Bomb, index, additive);
        ApplyGroup(data["burstSliders"], TrackModelState.Kind.BurstSlider, index, additive);
        ApplyGroup(data["burstSliderElements"], TrackModelState.Kind.BurstSliderElement, index, additive);
    }

    private void ApplyGroup(JSONNode data, TrackModelState.Kind kind, int index, bool additive)
    {
        if (data == null) return;
        foreach (var track in GetTracks(data["track"]))
        {
            ApplyModel(index, track, kind, data, "asset", additive);
            if (kind == TrackModelState.Kind.DirectionalNote)
                ApplyModel(index, track, TrackModelState.Kind.AnyNote, data, "anyDirectionAsset", additive);
        }
    }

    private static IEnumerable<string> GetTracks(JSONNode data)
    {
        if (data == null) yield break;
        if (data.IsString)
        {
            if (!string.IsNullOrEmpty(data.Value)) yield return data.Value;
            yield break;
        }

        if (!data.IsArray) yield break;
        var uniqueTracks = new HashSet<string>();
        foreach (var track in data.Children)
        {
            if (track.IsString && !string.IsNullOrEmpty(track.Value) && uniqueTracks.Add(track.Value))
                yield return track.Value;
        }
    }

    private void ApplyModel(
        int index,
        string track,
        TrackModelState.Kind kind,
        JSONNode json,
        string target,
        bool additive)
    {
        if (!json.HasKey(target)) return;
        var value = json[target];
        VisualModelSO asset = null;
        if (value != null)
        {
            if (value.Value == string.Empty) return;
            if (!VisualRepository.ModelsByName.TryGetValue(value.Value, out asset))
            {
                Debug.LogWarning($"Vivify prefab asset '{value.Value}' was not loaded.");
                return;
            }
        }

        if (!trackToTrackModelState.TryGetValue(track, out var trackModel))
            trackToTrackModelState[track] = trackModel = new TrackModelState();
        if (additive)
            trackModel.AddModel(kind, asset, index);
        else
            trackModel.SetModel(kind, asset, index);

        TracksManager.QueueModelUpdate(track);
    }

    public void Reset()
    {
        foreach (var (track, modelState) in trackToTrackModelState)
        {
            if (TracksManager != null) TracksManager.QueueModelUpdate(track);
            modelState.Reset();
        }

        trackToTrackModelState.Clear();
        cachedSet = default;
        cachedAdditive.Clear();
        cachedResult.OverrideModel = null;
        cachedResult.HasOverride = false;
        cachedResult.AdditiveModels.Clear();
    }
}
