using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapObstacleInputController : BeatmapInputController<BeatmapObstacleContainer>, CMInput.IObstacleObjectsActions
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;

    public void OnChangeWallDuration(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out BeatmapObstacleContainer obs);
        if (obs != null && !obs.dragging && context.performed)
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(obs.objectData);
            float snapping = 1f / atsc.gridMeasureSnapping;
            snapping *= context.ReadValue<float>() > 0 ? 1 : -1;
            obs.obstacleData._duration += snapping;
            obs.UpdateGridPosition();
            obstacleAppearanceSO.SetObstacleAppearance(obs);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(obs.objectData, obs.objectData, original));
        }
    }

    public void OnToggleHyperWall(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out BeatmapObstacleContainer obs);
        if (obs != null && !obs.dragging && context.performed)
        {
            ToggleHyperWall(obs);
        }
    }

    public void ToggleHyperWall(BeatmapObstacleContainer obs)
    {
        if (BeatmapObject.GenerateCopy(obs.objectData) is BeatmapObstacle edited)
        {
            edited._time += obs.obstacleData._duration;
            edited._duration *= -1f;

            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(edited, obs.objectData, obs.objectData), true);
        }
    }
}
