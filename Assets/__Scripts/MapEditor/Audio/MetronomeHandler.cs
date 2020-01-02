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
    
    [SerializeField] private GameObject metronomeUI;
    private RectTransform metronomeUITransform;

    private void Start()
    {
        metronomeUITransform = metronomeUI.GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        metronomeUI.SetActive(Settings.Instance.MetronomeVolume != 0f);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MeasureLine") && atsc.IsPlaying)
        {
            //Get Measure Name: other.GetComponent<TextMeshProUGUI>().text
            audioUtil.PlayOneShotSound(metronomeSound, Settings.Instance.MetronomeVolume);
            Vector3 vec = metronomeUITransform.localScale;
            vec.x = -vec.x;
            metronomeUITransform.localScale = vec;
        }
    }
}
