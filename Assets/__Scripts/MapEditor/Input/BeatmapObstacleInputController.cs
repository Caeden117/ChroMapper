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
    [FormerlySerializedAs("bpmChangesContainer")][SerializeField] private BPMChangeGridContainer bpmChangeGridContainer;
    [FormerlySerializedAs("obstacleAppearanceSO")][SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;

    public void OnChangeWallDuration(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragging && context.performed)
        {
            var original = BeatmapFactory.Clone(obs.ObjectData);
            var snapping = 1f / atsc.GridMeasureSnapping;
            snapping *= ((context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollWallDuration)
                ? 1
                : -1;

            obs.ObstacleData.Duration += snapping;
            obs.UpdateGridPosition();
            obstacleAppearanceSo.SetObstacleAppearance(obs);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(obs.ObjectData, obs.ObjectData, original));
        }
    }

    public void OnChangeWallLowerBound(InputAction.CallbackContext context)
    {
        if (Settings.Instance.MapVersion < 3 || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragging && context.performed)
        {
            var original = BeatmapFactory.Clone(obs.ObjectData);
            var tweakValue = ((context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollWallDuration)
                ? 1
                : -1;
            var data = obs.ObjectData as V3Obstacle;
            data.PosY = Mathf.Clamp(data.PosY + tweakValue, 0, 2);
            data.Height = Mathf.Min(data.Height, 5 - data.PosY);
            obs.UpdateGridPosition();
            obstacleAppearanceSo.SetObstacleAppearance(obs);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(obs.ObjectData, obs.ObjectData, original));
        }
    }
    public void OnChangeWallUpperBound(InputAction.CallbackContext context)
    {
        if (Settings.Instance.MapVersion < 3 || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragging && context.performed)
        {
            var original = BeatmapFactory.Clone(obs.ObjectData);
            var tweakValue = ((context.ReadValue<float>() > 0) ^ Settings.Instance.InvertScrollWallDuration)
                ? 1
                : -1;
            var data = obs.ObjectData as V3Obstacle;
            data.Height = Mathf.Clamp(data.Height + tweakValue, 1, 5 - data.PosY);
            obs.UpdateGridPosition();
            obstacleAppearanceSo.SetObstacleAppearance(obs);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(obs.ObjectData, obs.ObjectData, original));
        }
    }

    public void OnToggleHyperWall(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragging && context.performed) ToggleHyperWall(obs);
    }

    public void ToggleHyperWall(ObstacleContainer obs)
    {
        var wall = BeatmapFactory.Clone(obs.ObjectData) as BaseObstacle;
        wall.JsonTime += obs.ObstacleData.Duration;
        wall.Duration *= -1f;

        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(wall, obs.ObjectData, obs.ObjectData),
            true);
    }
}
