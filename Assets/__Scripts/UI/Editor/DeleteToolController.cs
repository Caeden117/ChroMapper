using System;
using UnityEngine;

public class DeleteToolController : MonoBehaviour
{
    public static event Action OnDeleteToolActivated;
    public static bool IsActive { get; private set; }

    public static void UpdateDeletion(bool active)
    {
        IsActive = active;
        OnDeleteToolActivated?.Invoke();
    }

    private void OnDestroy() => IsActive = false;

    public void ToggleDeletion() => UpdateDeletion(!IsActive);
}
