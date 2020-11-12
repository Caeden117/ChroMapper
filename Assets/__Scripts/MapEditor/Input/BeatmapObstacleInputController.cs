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
        if (obs != null && context.performed)
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(obs.objectData);
            float snapping = 1f / atsc.gridMeasureSnapping;
            snapping *= context.ReadValue<float>() > 0 ? 1 : -1;
            obs.obstacleData._duration += snapping;
            obs.UpdateGridPosition();
            obstacleAppearanceSO.SetObstacleAppearance(obs);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(obs.objectData), original));
        }
    }

    public void OnToggleHyperWall(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out BeatmapObstacleContainer obs);
        if (obs != null && context.performed)
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(obs.objectData);
            obs.obstacleData._time += obs.obstacleData._duration;
            obs.obstacleData._duration *= -1f;
            obstacleAppearanceSO.SetObstacleAppearance(obs);
            obs.UpdateGridPosition();
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(obs.objectData), original));
        }
    }
}
