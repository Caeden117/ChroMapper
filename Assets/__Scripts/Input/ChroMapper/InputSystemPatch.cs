using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using HarmonyLib;

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

    private static MethodInfo ReturnFromFunctionInfo = SymbolExtensions.GetMethodInfo(() => WillReturnFromFunction(null, null));

    private static IEnumerable<InputAction> allInputActions;
    private static Dictionary<InputAction, IEnumerable<string>> allInputBindingNames = new Dictionary<InputAction, IEnumerable<string>>();

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
                    new CodeInstruction(OpCodes.Ldloc_1),                      // Load InputActionMap
                    new CodeInstruction(OpCodes.Ldloc_2),                      // Load InputAction
                    new CodeInstruction(OpCodes.Call, ReturnFromFunctionInfo), // Call our method, which returns a bool
                    new CodeInstruction(OpCodes.Brtrue_S, returnOperand),      // Jump execution to the "return" instruction if the above method returns true.
                });
                break;
            }
        }
        return codes;
    }

    public static bool WillReturnFromFunction(InputActionMap map, InputAction action)
    {
        if (map.controlSchemes.Any(x => x.name.Contains("ChroMapper")) && action.controls.All(x => x is ButtonControl))
        {
            return allInputActions.Any(otherAction =>                                                                  // We prevent this action from triggering...
                    (otherAction.phase == InputActionPhase.Performed || otherAction.phase == InputActionPhase.Started) // If the other action has started or is performed,
                    && otherAction.controls.All(x => x.IsPressed())                                                    // All of the other controls are pressed,
                    && otherAction.bindings.Count > action.bindings.Count                                              // The other action has more bindings than we do,
                    && action.bindings.All(x => allInputBindingNames[otherAction].Contains(x.path)));                  // And all of our bindings exist in the other action.
        }
        return false;
    }
    
    private void Start()
    {
        allInputActions = CMInputCallbackInstaller.InputInstance.asset.actionMaps.SelectMany(x => x.actions.AsEnumerable());
        allInputBindingNames = allInputActions.ToDictionary(x => x, x => x.bindings.Select(y => y.path));

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
