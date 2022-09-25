using System.Collections;
using QuestDumper;
using UnityEngine;

// This class is so stupid and I don't know why I made it so stupid
// I'm sorry
[RequireComponent(typeof(BetterToggle))]
public class AdbHandler : MonoBehaviour
{
    private BetterToggle betterToggle;

    private void Start()
    {
        betterToggle = GetComponent<BetterToggle>();
        // Set toggle

        betterToggle.IsOn = Adb.IsAdbInstalled(out _);
        
        // Force Update UI ugh
        betterToggle.UpdateUI();
    }

    /// <summary>
    /// Toggles ADB installation
    ///
    /// This in reality is just for downloading ADB,
    /// would rather it be a button and hidden when it is installed
    ///
    /// </summary>
    public void ToggleADB() =>
        // Remove ADB if already installed and toggled
        StartCoroutine(ToggleADBCoroutine(Adb.IsAdbInstalled(out _) ? AdbUI.DoRemove() : AdbUI.DoDownload()));

    private IEnumerator ToggleADBCoroutine(IEnumerator enumerator)
    {
        yield return enumerator;
        
        betterToggle.SetUiOn(Adb.IsAdbInstalled(out _), false);
    }
}

