using System.Collections;
using QuestDumper;
using UnityEngine;

// This class is so stupid and I don't know why I made it so stupid
// I'm sorry
public class AdbHandler : MonoBehaviour
{
    private BetterToggle _betterToggle;

    // I hate this
    private bool toggledBySelf;
    
    private void Start()
    {
        _betterToggle = GetComponent<BetterToggle>();
        // Set toggle

        
        // Update UI ugh
        toggledBySelf = true;
        _betterToggle.SetUiOn(Adb.IsAdbInstalled(out _));
        toggledBySelf = false;
    }

    /// <summary>
    /// Toggles ADB installation
    ///
    /// This in reality is just for downloading ADB,
    /// would rather it be a button and hidden when it is installed
    ///
    /// </summary>
    public void ToggleADB()
    {
        // don't do stuff on awake call (or when updating the UI smh smh smh smh)
        if (toggledBySelf) return;
        toggledBySelf = true;
        
        // Remove ADB if already installed and toggled
        StartCoroutine(ToggleADBCoroutine(Adb.IsAdbInstalled(out _) ? AdbUI.DoRemove() : AdbUI.DoDownload()));
    }

    private IEnumerator ToggleADBCoroutine(IEnumerator enumerator)
    {
        yield return enumerator;
        
        _betterToggle.SetUiOn(Adb.IsAdbInstalled(out _));
        toggledBySelf = false;
    }
}

