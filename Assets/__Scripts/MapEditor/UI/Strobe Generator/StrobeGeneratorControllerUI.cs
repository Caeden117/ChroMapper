using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem;
using System.Collections;

public class StrobeGeneratorControllerUI : MonoBehaviour, CMInput.IStrobeGeneratorActions
{
    [SerializeField] private VerticalLayoutGroup settingsPanelList;
    [SerializeField] private StrobeGenerator strobeGen;

    private StrobeGeneratorPassUIController[] allPassUIControllers;

    private void Start()
    {
        allPassUIControllers = GetComponentsInChildren<StrobeGeneratorPassUIController>();
    }

    public void GenerateStrobeWithUISettings()
    {
        List<StrobeGeneratorPass> passes = new List<StrobeGeneratorPass>();

        foreach (StrobeGeneratorPassUIController activePass in allPassUIControllers.Where(x => x.WillGenerate))
        {
            passes.Add(activePass.GetPassForGeneration());
        }

        strobeGen.GenerateStrobe(passes);
    }

    public void OnQuickStrobeGen(InputAction.CallbackContext context)
    {
        if (context.performed) GenerateStrobeWithUISettings();
    }

    // Unity is a fantastic game engine with no flaws whatsoever.
    // Just kidding. It's shit. This shouldn't be necessary. Why am I being forced to go this route so that Unity UI can update the way that it's supposed to god fucking damnit i have lost all hope in the unity engine by spending one hour of my life just to waste a frame (and get a flickering effect) by having to write this ienumerator god dufkcinhjslkajdfklwa
    private IEnumerator DirtySettingsList()
    {
        settingsPanelList.enabled = false;
        yield return new WaitForEndOfFrame();
        settingsPanelList.enabled = true;
    }
}
