using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using HarmonyLib;
using System.Runtime.InteropServices;
using System.Text;

/*
 * Oh boy, another ChroMapper Harmony patch!
 * 
 * Why does this one need to exist?
 * 
 * Say you have two keybinds, bound to "S", and "Shift + S". If you were to press Shift and S on your keyboard, you'd expect only the latter keybind to trigger, right?
 * 
 * WRONG! Unity's new Input System triggers both, because you're still technically pressing S! This is outrageous, and must be swiftly resolved with a Harmony patch!
 */
public class InputSystemPatch : MonoBehaviour
{
    private const string inputPatchID = "com.caeden117.chromapper.inputpatch";

    private static MethodInfo ReturnFromFunctionInfo = SymbolExtensions.GetMethodInfo(() => WillReturnFromFunction(null));

    private static IEnumerable<InputAction> allInputActions;
    private static Dictionary<InputAction, IEnumerable<string>> allInputBindingNames = new Dictionary<InputAction, IEnumerable<string>>();
    private static IEnumerable<InputControl> allControls;

    private static IEnumerable<string> ignoredPaths = new List<string>()
    {
        "<Pointer>/position",
        "<Mouse>/delta",
        "<Mouse>/press",
        "<Keyboard>/anyKey"
    };

    private Harmony inputPatchHarmony;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Switch) // Catch our Switch statement in the original function
            {
                object returnOperand = codes[codes.Count - 2].operand; // Grab the address for the "return" instruction, which can be located at the 2nd to last instruction.

                codes.InsertRange(i - 3, new List<CodeInstruction>() // Take a few steps back and inject our code
                {
                    new CodeInstruction(OpCodes.Ldloc_2),                      // Load InputAction
                    new CodeInstruction(OpCodes.Call, ReturnFromFunctionInfo), // Call our method, which returns a bool
                    new CodeInstruction(OpCodes.Brtrue_S, returnOperand),      // Jump execution to the "return" instruction if the above method returns true.
                });
                break;
            }
        }
        return codes;
    }

    public static bool WillReturnFromFunction(InputAction action)
    {
        if (action.phase != InputActionPhase.Started && action.phase != InputActionPhase.Performed) return false;

        if (!action.actionMap.controlSchemes.Any(c => c.name.Contains("ChroMapper"))) return false;

        if (action.bindings.Any(b => ignoredPaths.Contains(b.path))) return false;

        return allInputActions.Any((otherAction) => {

            // Just a whole bunch of conditions to short circuit this particular check
            if (action.id == otherAction.id
                || CMInputCallbackInstaller.IsActionMapDisabled(otherAction.GetType())
                || otherAction.bindings.Any(b => ignoredPaths.Contains(b.path))
                || (otherAction.phase != InputActionPhase.Started && otherAction.phase != InputActionPhase.Performed)
                || !allInputBindingNames.TryGetValue(action, out var paths)
                || !allInputBindingNames.TryGetValue(otherAction, out var otherPaths)) return false;

            // All controls on the other action must be activated
            bool allControlsPressed = otherAction.controls.All(x => x.IsPressed() || x.IsActuated());
            // The other action must contain more bindings than the current action does
            bool moreBindings = otherPaths.Count() > paths.Count();

            bool result = allControlsPressed && moreBindings;
            
            /*
            if (result)
            {
                Debug.Log($"{action.name} blocked by {otherAction.name}: {allControlsPressed} | {moreBindings}");
            }*/

            return result;
        });
    }

    // fuck you unity for making input system paths inconsistent
    private static bool CheckEqualPaths(string pathA, string pathB)
    {
        var splitPathA = StripString(pathA, '<', '>', '/');
        var splitPathB = StripString(pathB, '<', '>', '/');

        return splitPathA == splitPathB;
    }

    private static string StripString(string source, params char[] toRemove)
    {
        var newString = new StringBuilder();

        newString.Append(source.Where(ch => !toRemove.Contains(ch)));

        return newString.ToString();
    }
    
    private void Start()
    {
        allInputActions = CMInputCallbackInstaller.InputInstance.asset.actionMaps.SelectMany(x => x.actions);
        allInputBindingNames = allInputActions.ToDictionary(x => x, x => x.bindings.Select(y => y.path));
        allControls = InputSystem.devices.SelectMany(d => d.allControls.Where(c => c is KeyControl || c is ButtonControl));

        Type InputActionStateType = Assembly.GetAssembly(typeof(InputSystem)).GetTypes().First(x => x.Name == "InputActionState");
        MethodInfo original = InputActionStateType.GetMethod("ChangePhaseOfActionInternal", BindingFlags.NonPublic | BindingFlags.Instance);

        inputPatchHarmony = new Harmony(inputPatchID);
        inputPatchHarmony.Patch(original, null, null, new HarmonyMethod(GetType(), nameof(Transpiler)));
    }

    private void OnDestroy()
    {
        inputPatchHarmony.UnpatchAll();
    }
}
