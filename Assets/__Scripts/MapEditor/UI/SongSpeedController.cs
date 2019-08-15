using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SongSpeedController : MonoBehaviour {

    public AudioSource source;

	public void UpdateSongSpeed(float speedValue)
    {
        source.pitch = (speedValue / 10);
    }
}
