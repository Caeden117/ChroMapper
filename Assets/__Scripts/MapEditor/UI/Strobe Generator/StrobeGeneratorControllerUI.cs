using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StrobeGeneratorControllerUI : MonoBehaviour, CMInput.IStrobeGeneratorActions
{
    [SerializeField] private VerticalLayoutGroup settingsPanelList;
    [SerializeField] private StrobeGenerator strobeGen;

    private StrobeGeneratorPassUIController[] allPassUIControllers;

    private void Start() => allPassUIControllers = GetComponentsInChildren<StrobeGeneratorPassUIController>();

    public void OnQuickStrobeGen(InputAction.CallbackContext context)
    {
        if (context.performed) GenerateStrobeWithUISettings();
    }

    public void GenerateStrobeWithUISettings()
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.ShowDialogBox("Mapper", "gradient.error",
                null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        var passes = new List<StrobeGeneratorPass>();

        foreach (var activePass in allPassUIControllers.Where(x => x.WillGenerate))
            passes.Add(activePass.GetPassForGeneration());

        strobeGen.GenerateStrobe(passes);
    }


    // Unity is a fantastic game engine with no flaws whatsoever.
    // Just kidding. It's shit. This shouldn't be necessary. Why am I being forced to go this route so that Unity UI can update the way that it's supposed to god fucking damnit i have lost all hope in the unity engine by spending one hour of my life just to waste a frame (and get a flickering effect) by having to write this ienumerator god dufkcinhjslkajdfklwa
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
        Justification = "This is called indirectly via Unity Message.")]
    private IEnumerator DirtySettingsList()
    {
        settingsPanelList.enabled = false;
        yield return new WaitForEndOfFrame();
        settingsPanelList.enabled = true;
    }
}
