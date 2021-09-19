using System;
using System.Collections.Generic;
using UnityEngine;

public class HideGridOnUnloadedObjects : MonoBehaviour
{
    [Header("Invisibility Flags")]
    [Header("All selected object types must be disabled\nfor this GameObject to be disabled.")]
    [SerializeField]
    private bool notes;

    [SerializeField] private bool obstacles;
    [SerializeField] private bool events;
    [SerializeField] private bool otherObjects;

    private readonly List<(string name, Func<bool> func)> visibilityFlags = new List<(string, Func<bool>)>();

    private void Start()
    {
        if (notes) RegisterFlag(nameof(Settings.Load_Notes), () => Settings.Instance.Load_Notes);

        if (obstacles) RegisterFlag(nameof(Settings.Load_Obstacles), () => Settings.Instance.Load_Obstacles);

        if (events) RegisterFlag(nameof(Settings.Load_Events), () => Settings.Instance.Load_Events);

        if (otherObjects) RegisterFlag(nameof(Settings.Load_Others), () => Settings.Instance.Load_Others);

        Refresh();
    }

    private void OnDestroy()
    {
        foreach (var (name, _) in visibilityFlags) Settings.ClearSettingNotifications(name);

        visibilityFlags.Clear();
    }

    private void RegisterFlag(string name, Func<bool> value)
    {
        visibilityFlags.Add((name, value));
        Settings.NotifyBySettingName(name, Refresh);
    }

    private void Refresh(object _ = null)
    {
        gameObject.SetActive(!visibilityFlags.TrueForAll(x => !x.func()));
        GridOrderController.MarkDirty();
    }
}
