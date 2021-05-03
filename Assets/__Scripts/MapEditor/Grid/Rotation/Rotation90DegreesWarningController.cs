using UnityEngine;
using TMPro;

public class Rotation90DegreesWarningController : MonoBehaviour
{
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private RotationCallbackController rotationCallback;
    [SerializeField] private TextMeshProUGUI rotationDisplay;

    // Start is called before the first frame update
    private void Start()
    {
        if (BeatSaberSongContainer.Instance.difficultyData.parentBeatmapSet.beatmapCharacteristicName == "90Degree")
        {
            rotationCallback.RotationChangedEvent += RotationChangedEvent;
        }
    }

    private void RotationChangedEvent(bool natural, int rotation)
    {
        rotationDisplay.color = (rotation < -45 || rotation > 45) ? Color.red : Color.white;
    }

    private void OnDestroy()
    {
        if (BeatSaberSongContainer.Instance.difficultyData.parentBeatmapSet.beatmapCharacteristicName == "90Degree")
        {
            rotationCallback.RotationChangedEvent -= RotationChangedEvent;
        }
    }
}
