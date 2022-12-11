using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapObstacleInputController : BeatmapInputController<BeatmapObstacleContainer>,
    CMInput.IObstacleObjectsActions
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BPMChangesContainer bpmChangesContainer;
    [FormerlySerializedAs("obstacleAppearanceSO")] [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;

    public void OnChangeWallDuration(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragging && context.performed)
        {
            var original = BeatmapObject.GenerateCopy(obs.ObjectData);
            var snapping = 1f / atsc.GridMeasureSnapping;
            snapping *= context.ReadValue<float>() > 0 ? 1 : -1;

            var wallEndTime = obs.ObstacleData.Time + obs.ObstacleData.Duration;
            var durationTweak = bpmChangesContainer.LocalBeatsToSongBeats(snapping, wallEndTime);

            obs.ObstacleData.Duration += durationTweak;
            obs.UpdateGridPosition();
            obstacleAppearanceSo.SetObstacleAppearance(obs);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(obs.ObjectData, obs.ObjectData, original));
        }
    }

    public void OnToggleHyperWall(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out var obs);
        if (obs != null && !obs.Dragging && context.performed) ToggleHyperWall(obs);
    }

    public void ToggleHyperWall(BeatmapObstacleContainer obs)
    {
        if (BeatmapObject.GenerateCopy(obs.ObjectData) is BeatmapObstacle edited)
        {
            edited.Time += obs.ObstacleData.Duration;
            edited.Duration *= -1f;

            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(edited, obs.ObjectData, obs.ObjectData),
                true);
        }
    }
}
