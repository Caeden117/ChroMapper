using UnityEngine;

public class SongSpeedController : MonoBehaviour {

    public AudioSource source;

	public void UpdateSongSpeed(float speedValue)
    {
        source.pitch = (speedValue / 10);
    }
}
