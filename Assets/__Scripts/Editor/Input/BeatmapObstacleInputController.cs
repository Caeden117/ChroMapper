using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapObstacleInputController : BeatmapInputController<ObstacleContainer>,
                                              CMInput.IObstacleObjectsActions
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BPMChangeGridContainer bpmChangeGridContainer;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    public void OnChangeWallDuration(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var obs);
        if (obs == null || obs.Dragged || !context.performed) return;

        var snapping = 1f / atsc.GridMeasureSnapping;
        snapping *= context.GetScrollDirection(Settings.Instance.InvertScrollWallDuration);
        TweakDuration(obs, snapping);
    }

    public void TweakDuration(ObstacleContainer obs, float durationDelta)
    {
        var original = BeatmapFactory.Clone(obs.ObjectData);
        obs.ObstacleData.Duration += durationDelta;
        obs.UpdateGridPosition();
        obstacleAppearanceSo.SetObstacleAppearance(obs, beatmapRuntimeContext);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                obs.ObjectData,
                obs.ObjectData,
                original,
                mergeType: ActionMergeType.WallDurationTweak,
                preserveSelection: true));
    }

    public void OnChangeWallLowerBound(InputAction.CallbackContext context)
    {
        if (Settings.Instance.MapVersion < 3
            || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return;
        RaycastFirstObject(out var obs);
        if (obs == null || obs.Dragged || !context.performed) return;

        TweakLowerBound(obs, context.GetScrollDirection(Settings.Instance.InvertScrollWallDuration));
    }

    public void TweakLowerBound(ObstacleContainer obs, int tweakValue)
    {
        var original = BeatmapFactory.Clone(obs.ObjectData);
        var data = obs.ObjectData as BaseObstacle;
        data.PosY = Mathf.Clamp(data.PosY + tweakValue, 0, 2);
        data.Height = Mathf.Min(data.Height, 5 - data.PosY);
        if (data.CompareTo(original) == 0) return;
        obs.UpdateGridPosition();
        obstacleAppearanceSo.SetObstacleAppearance(obs, beatmapRuntimeContext);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                obs.ObjectData,
                obs.ObjectData,
                original,
                mergeType: ActionMergeType.WallLowerBoundTweak,
                preserveSelection: true));
    }

    public void OnChangeWallUpperBound(InputAction.CallbackContext context)
    {
        if (Settings.Instance.MapVersion < 3
            || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return;
        RaycastFirstObject(out var obs);
        if (obs == null || obs.Dragged || !context.performed) return;

        TweakUpperBound(obs, context.GetScrollDirection(Settings.Instance.InvertScrollWallDuration));
    }

    public void TweakUpperBound(ObstacleContainer obs, int tweakValue)
    {
        var original = BeatmapFactory.Clone(obs.ObjectData);
        var data = obs.ObjectData as BaseObstacle;
        data.Height = Mathf.Clamp(data.Height + tweakValue, 1, 5 - data.PosY);
        if (data.CompareTo(original) == 0) return;
        obs.UpdateGridPosition();
        obstacleAppearanceSo.SetObstacleAppearance(obs, beatmapRuntimeContext);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                obs.ObjectData,
                obs.ObjectData,
                original,
                mergeType: ActionMergeType.WallUpperBoundTweak,
                preserveSelection: true));
    }

    public void OnToggleHyperWall(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragged && context.performed) ToggleHyperWall(obs);
    }

    public void ToggleHyperWall(ObstacleContainer obs)
    {
        var wall = BeatmapFactory.Clone(obs.ObjectData) as BaseObstacle;
        wall.JsonTime += obs.ObstacleData.Duration;
        wall.Duration *= -1f;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(wall, obs.ObjectData, obs.ObjectData, preserveSelection: true),
            true);
    }
}
