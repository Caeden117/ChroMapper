using System;
using System.Collections.Generic;
using UnityEngine;

public class DeleteToolController : MonoBehaviour
{
    public static bool IsActive { get; private set; } = false;

    public static Action DeleteToolActivatedEvent;

    public void UpdateDeletion(bool enabled)
    {
        IsActive = enabled;
        if (enabled) DeleteToolActivatedEvent?.Invoke();
    } 

    public void ToggleDeletion()
    {
        UpdateDeletion(!IsActive);
    }
}
