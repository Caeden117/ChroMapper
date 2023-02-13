using TMPro;
using UnityEngine;

public class Rotation90DegreesWarningController : MonoBehaviour
{
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private RotationCallbackController rotationCallback;
    [SerializeField] private TextMeshProUGUI rotationDisplay;

    // Start is called before the first frame update
    private void Start()
    {
        if (BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet.BeatmapCharacteristicName == "90Degree")
            rotationCallback.RotationChangedEvent += RotationChangedEvent;
    }

    private void OnDestroy()
    {
        if (BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet.BeatmapCharacteristicName == "90Degree")
            rotationCallback.RotationChangedEvent -= RotationChangedEvent;
    }

    private void RotationChangedEvent(bool natural, float rotation) =>
        rotationDisplay.color = rotation < -45f || rotation > 45f ? Color.red : Color.white;
}
