using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SongSpeedController : MonoBehaviour {

    [SerializeField] private AudioSource source;
    [SerializeField] private TextMeshProUGUI display;

	public void UpdateSongSpeed(float speedValue)
    {
        source.pitch = (speedValue / 10);
        display.text = source.pitch * 100 + "%";
    }
}
