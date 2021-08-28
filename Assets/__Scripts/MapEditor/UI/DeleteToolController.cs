using System;
using UnityEngine;

public class DeleteToolController : MonoBehaviour
{
    public static Action DeleteToolActivatedEvent;
    public static bool IsActive { get; private set; }

    public void UpdateDeletion(bool enabled)
    {
        IsActive = enabled;
        if (enabled) DeleteToolActivatedEvent?.Invoke();
    }

    public void ToggleDeletion() => UpdateDeletion(!IsActive);
}
