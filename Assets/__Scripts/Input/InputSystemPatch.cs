using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/*
 * Oh boy, another ChroMapper Harmony patch!
 * 
 * Why does this one need to exist?
 * 
 * Say you have two keybinds, bound to "S", and "Shift + S". If you were to press Shift and S on your keyboard, you'd expect only the latter keybind to trigger, right?
 * 
 * WRONG! Unity's new Input System triggers both, because you're still technically pressing S! This is outrageous, and must be swiftly resolved with a Harmony patch!
 *
 * Update: As of InputSystem 1.7.0, keybinds bound to multiple buttons can now consume inputs and fixes this.
 *         BUT this means we can't have overlapping keybinds that use multiple buttons. If you bind "Shift + S"
 *         to perform multiple actions, only one of the actions will trigger... so we still need this patch.
 *         TODO: Investigate if CMInputCallbackInstaller is the reason for this issue
 */
public class InputSystemPatch : MonoBehaviour
{
    private const string inputPatchID = "com.caeden117.chromapper.inputpatch";

    private static readonly MethodInfo returnFromFunctionInfo =
        SymbolExtensions.GetMethodInfo(() => WillReturnFromFunction(null));

    private static IEnumerable<InputAction> allInputActions;

    private static Dictionary<InputAction, string[]> allInputBindingNames = new();

    private static IEnumerable<InputControl> allControls;

    private static Dictionary<string, InputControl> resolvedPathControls = new();

    // Key 1: Interrogated InputAction | Value: InputActions that have the possibility of blocking the interrogated action
    private static readonly ConcurrentDictionary<InputAction, List<InputAction>> inputActionBlockMap = new();

    private static CMInput blockMapInput;

    private Harmony inputPatchHarmony;

    private void Start()
    {
        RebuildBlockMap();

        var inputActionStateType = Assembly.GetAssembly(typeof(InputSystem)).GetTypes()
            .First(x => x.Name == "InputActionState");
        var original = inputActionStateType.GetMethod("ChangePhaseOfActionInternal",
            BindingFlags.NonPublic | BindingFlags.Instance);

        inputPatchHarmony = new Harmony(inputPatchID);
        inputPatchHarmony.Patch(original, null, null, new HarmonyMethod(GetType(), nameof(Transpiler)));
    }

    // Rebuild the blocker map from the installed production asset, for example after a hard keybind reset.
    public static void RebuildBlockMap()
    {
        var input = CMInputCallbackInstaller.InputInstance;
        if (input == null)
        {
            return;
        }

        blockMapInput = input;
        allInputActions = input.asset.actionMaps
            .SelectMany(x => x.actions)
            .ToList();
        allInputBindingNames = allInputActions
            .ToDictionary(x => x, x => x.bindings
                .Where(y => !y.isComposite)
                .Select(y => y.path)
                .ToArray());
        allControls = InputSystem.devices
            .SelectMany(d => d.allControls
                .Where(c => c is KeyControl or ButtonControl));

        // Resolve every binding path once; the pairwise blocker pass would otherwise search for the
        // same control millions of times.
        resolvedPathControls = allInputBindingNames.Values
            .SelectMany(paths => paths)
            .Distinct()
            .ToDictionary(path => path, path => InputSystem.FindControl(path));

        // I cant believe this actually worked first try
        // I'm pretty much caching a map of actions that can block each other, doing the heavy lifting on separate threads.
        inputActionBlockMap.Clear();
        Parallel.ForEach(allInputActions, action =>
        {
            if (action is null) return;
            var map = new ConcurrentBag<InputAction>();
            Parallel.ForEach(allInputActions, other =>
            {
                if (other is null) return;
                if (WillBeBlockedByAction(action, other)) map.Add(other);
            });
            inputActionBlockMap.TryAdd(action, map.ToList());
        });
    }

    // A runtime rebind replaces bindings on the installed asset, so the cached blocker map must be
    // rebuilt. Temporary assets used by tests and rebind fixtures must not replace the production map.
    public static void RefreshBlockMap(CMInput changedInput)
    {
        if (blockMapInput != null && ReferenceEquals(changedInput, blockMapInput))
        {
            RebuildBlockMap();
        }
    }

    private void OnDestroy() => inputPatchHarmony?.UnpatchSelf();

    private static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator,
        IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Switch) // Catch our Switch statement in the original function
            {
                var returnLabel = generator.DefineLabel();
                codes[^1] = codes[^1].WithLabels(returnLabel);

                codes.InsertRange(i - 3, new List<CodeInstruction>() // Take a few steps back and inject our code
                {
                    new CodeInstruction(OpCodes.Ldloc_2), // Load InputAction
                    new CodeInstruction(OpCodes.Call, returnFromFunctionInfo), // Call our method, which returns a bool
                    new CodeInstruction(OpCodes.Brtrue_S,
                        returnLabel) // Jump execution to the "return" instruction if the above method returns true.
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

    private static bool CheckEqualPaths(string pathA, string pathB)
    {
        // Xvfb may expose bindings without matching devices or controls.
        if (string.Equals(pathA, pathB, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB))
        {
            return false;
        }

        var controlA = ResolveControl(pathA);
        var controlB = ResolveControl(pathB);
        return controlA != null && ReferenceEquals(controlA, controlB);
    }

    private static InputControl ResolveControl(string path) =>
        resolvedPathControls.TryGetValue(path, out var control)
            ? control
            : InputSystem.FindControl(path);

    private static string StripString(string source, params char[] toRemove)
    {
        var newString = new StringBuilder();

        newString.Append(source.Where(ch => !toRemove.Contains(ch)));

        return newString.ToString();
    }

    private static bool WillBeBlockedByAction(InputAction action, InputAction otherAction)
    {
        var schemes = action.actionMap.controlSchemes;
        var usesChroMapperScheme = false;
        for (var i = 0; i < schemes.Count && !usesChroMapperScheme; i++)
        {
            usesChroMapperScheme = schemes[i].name.Contains("ChroMapper");
        }

        if (!usesChroMapperScheme)
        {
            return false;
        }

        var bindings = action.bindings;
        for (var i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].action.StartsWith(KeybindsController.PersistentKeybindIdentifier))
            {
                return false;
            }
        }

        // Pulse width arbitrates timeline scroll ownership per event instead of blocking it globally.
        if (otherAction.id == StrobePulseWidthInput.PulseWidthActionId)
        {
            return false;
        }

        // Just a whole bunch of conditions to short circuit this particular check
        if (action.id == otherAction.id
            || !allInputBindingNames.TryGetValue(action, out var paths)
            || !allInputBindingNames.TryGetValue(otherAction, out var otherPaths)
            || otherPaths.Length <= paths.Length)
        {
            return false;
        }

        // The other action must contain every binding the current action does.
        for (var i = 0; i < paths.Length; i++)
        {
            var matched = false;
            for (var j = 0; j < otherPaths.Length && !matched; j++)
            {
                matched = CheckEqualPaths(paths[i], otherPaths[j]);
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }
}
