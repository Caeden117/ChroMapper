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

            var bpmChange = bpmChangesContainer.FindLastBpm(wallEndTime);

            var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var bpmRatio = songBpm / (bpmChange?.Bpm ?? songBpm);
            var durationTweak = snapping * bpmRatio;

            var nextBpm = bpmChangesContainer.FindLastBpm(wallEndTime + durationTweak);

            if (nextBpm != bpmChange)
            {
                if (snapping > 0)
                {
                    durationTweak = nextBpm.Time - wallEndTime;
                }
                else
                {
                    // I dont think any solution here will please everyone so i'll just go with my intuition
                    durationTweak = bpmChangesContainer.FindRoundedBpmTime(wallEndTime + durationTweak, snapping * -1) - wallEndTime;
                }
            }

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
