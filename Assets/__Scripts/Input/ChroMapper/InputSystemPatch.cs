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
using System.Threading.Tasks;
using System.Collections.Concurrent;

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

    // Key 1: Interrogated InputAction | Value: InputActions that have the possibility of blocking the interrogated action
    private static readonly ConcurrentDictionary<InputAction, List<InputAction>> inputActionBlockMap = new ConcurrentDictionary<InputAction, List<InputAction>>();

    private Harmony inputPatchHarmony;

    private static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Switch) // Catch our Switch statement in the original function
            {
                var returnLabel = generator.DefineLabel();
                codes[codes.Count - 1].labels.Add(returnLabel);

                codes.InsertRange(i - 3, new List<CodeInstruction>() // Take a few steps back and inject our code
                {
                    new CodeInstruction(OpCodes.Ldloc_2),                      // Load InputAction
                    new CodeInstruction(OpCodes.Call, ReturnFromFunctionInfo), // Call our method, which returns a bool
                    new CodeInstruction(OpCodes.Brtrue_S, returnLabel),      // Jump execution to the "return" instruction if the above method returns true.
                });

                break;
            }
        }
        return codes;
    }

    // Now your FPS no longer drops to like 30 or something when spamming keys
    public static bool WillReturnFromFunction(InputAction action)
    {
        if (!inputActionBlockMap.TryGetValue(action, out var blockingActions)) return false;

        foreach (var otherAction in blockingActions)
        {
            if (CMInputCallbackInstaller.IsActionMapDisabled(otherAction.GetType())) continue;

            if (otherAction.controls.All(x => x.IsPressed() || x.IsActuated())) return true;
        }

        return false;
    }

    // fuck you unity for making input system paths inconsistent
    private static bool CheckEqualPaths(string pathA, string pathB)
    {
        return InputSystem.FindControl(pathA).GetHashCode() == InputSystem.FindControl(pathB).GetHashCode();
    }

    private static string StripString(string source, params char[] toRemove)
    {
        var newString = new StringBuilder();

        newString.Append(source.Where(ch => !toRemove.Contains(ch)));

        return newString.ToString();
    }

    private static bool WillBeBlockedByAction(InputAction action, InputAction otherAction)
    {
        if (!action.actionMap.controlSchemes.Any(c => c.name.Contains("ChroMapper"))) return false;

        // Just a whole bunch of conditions to short circuit this particular check
        if (action.id == otherAction.id
            || !allInputBindingNames.TryGetValue(action, out var paths)
            || !allInputBindingNames.TryGetValue(otherAction, out var otherPaths)) return false;

        // The other action must contain more bindings than the current action does
        bool moreBindings = otherPaths.Count() > paths.Count();
        bool sameBindings = paths.All(p1 => otherPaths.Any(p2 => CheckEqualPaths(p1, p2)));

        return moreBindings && sameBindings;
    }

    private void Start()
    {
        allInputActions = CMInputCallbackInstaller.InputInstance.asset.actionMaps.SelectMany(x => x.actions);
        allInputBindingNames = allInputActions.ToDictionary(x => x, x => x.bindings.Where(y => !y.isComposite).Select(y => y.path));
        allControls = InputSystem.devices.SelectMany(d => d.allControls.Where(c => c is KeyControl || c is ButtonControl));

        // I cant believe this actually worked first try
        // I'm pretty much caching a map of actions that can block each other, doing the heavy lifting on separate threads.
        Parallel.ForEach(allInputActions, (action) =>
        {
            if (action is null) return;
            var map = new ConcurrentBag<InputAction>();
            Parallel.ForEach(allInputActions, (other) =>
            {
                if (other is null) return;
                if (WillBeBlockedByAction(action, other))
                {
                    map.Add(other);
                }
            });
            inputActionBlockMap.TryAdd(action, map.ToList());
        });

        Type InputActionStateType = Assembly.GetAssembly(typeof(InputSystem)).GetTypes().First(x => x.Name == "InputActionState");
        MethodInfo original = InputActionStateType.GetMethod("ChangePhaseOfActionInternal", BindingFlags.NonPublic | BindingFlags.Instance);

        inputPatchHarmony = new Harmony(inputPatchID);
        inputPatchHarmony.Patch(original, null, null, new HarmonyMethod(GetType(), nameof(Transpiler)));
    }

    private void OnDestroy()
    {
        inputPatchHarmony?.UnpatchAll();
    }
}
