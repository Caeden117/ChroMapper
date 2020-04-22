using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReflectionProbeSettingUpdate : MonoBehaviour
{
    [SerializeField] private ReflectionProbe probe;

    void Start()
    {
        Settings.NotifyBySettingName("Reflections", UpdateReflectionSetting);
        UpdateReflectionSetting(Settings.Instance.Reflections);
    }

    private void UpdateReflectionSetting(object obj)
    {
        probe.enabled = (bool)obj;
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("Reflections");    
    }
}
