using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapObstacleInputController : BeatmapInputController<BeatmapObstacleContainer>, CMInput.IObstacleObjectsActions
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;

    public void OnChangeWallDuration(InputAction.CallbackContext context)
    {
        if (!KeybindsController.AltHeld) return;
        RaycastFirstObject(out BeatmapObstacleContainer obs);
        if (obs != null)
        {
            float snapping = 1f / atsc.gridMeasureSnapping;
            snapping *= context.ReadValue<float>() > 0 ? 1 : -1;
            obs.obstacleData._duration += snapping;
            obs.UpdateGridPosition();
            obstacleAppearanceSO.SetObstacleAppearance(obs);
        }
    }

    public void OnToggleHyperWall(InputAction.CallbackContext context)
    {
        if (KeybindsController.AnyCriticalKeys) return;
        RaycastFirstObject(out BeatmapObstacleContainer obs);
        if (obs != null)
        {
            obs.obstacleData._time += obs.obstacleData._duration;
            obs.obstacleData._duration *= -1f;
            obstacleAppearanceSO.SetObstacleAppearance(obs);
            obs.UpdateGridPosition();
        }
    }
}
