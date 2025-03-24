using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class GagaDiskManager : MonoBehaviour
{
    public float minPositionY = 0;
    public float maxPositionY = 8;
    public float moveSpeed = 10;

    public List<GagaDisk> Disks = new List<GagaDisk>();

    public void Awake()
    {
        foreach (var disk in Disks) 
        {
            Debug.Log("Disk: " + disk);
            disk.Init(20); // Start at Y 20 (default).
        }
    }

    private void FixedUpdate()
    {
        foreach (var disk in Disks)
        {
            disk.FixedUpdateDisk(Time.fixedDeltaTime);
        }
    }

    private void LateUpdate()
    {
        foreach (var disk in Disks)
        {
            disk.LateUpdateDisk(Time.deltaTime);
        }
    }

    public void HandlePositionEvent(BaseEvent evt)
    {
        var value = evt.Value; // Todo: Verify this is the correct property to use.
        var fValue = evt.FloatValue;
        Disks.Where(d => d.HeightEventType == evt.Type)
            .ToList()
            .ForEach(d => d.SetPosition(evt.Value * 6f, moveSpeed));
    }
}
