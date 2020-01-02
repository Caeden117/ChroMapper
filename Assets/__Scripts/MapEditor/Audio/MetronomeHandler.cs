using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MetronomeHandler : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioClip metronomeSound;

    [SerializeField] private AudioUtil audioUtil;
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MeasureLine") && atsc.IsPlaying)
        {
            //Get Measure Name: other.GetComponent<TextMeshProUGUI>().text
            audioUtil.PlayOneShotSound(metronomeSound, Settings.Instance.MetronomeVolume);
        }
    }
}
