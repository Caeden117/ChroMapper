using System;
using UnityEngine.UI;
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
