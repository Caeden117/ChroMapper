using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

// Unity dispatches every binding whose controls are currently active, so a plain wheel action also
// performs while a modifier chord is held. Actions in the same map arbitrate from their live bindings
// here, which keeps remapped chords working without reading physical modifier keys.
public static class InputChordOwnership
{
    public static bool Owns(InputAction.CallbackContext context) => Owns(context.action, context.control);

    public static bool Owns(InputAction action, InputControl triggeringControl)
    {
        if (action == null || !action.enabled || triggeringControl == null)
        {
            return false;
        }

        var bindings = action.bindings;
        for (var i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].isPartOfComposite)
            {
                continue;
            }

            var partEnd = CompositePartEnd(bindings, i);
            if (ChordActive(action, bindings, i, partEnd, triggeringControl)
                && !MoreSpecificSiblingOwns(action, i, partEnd, triggeringControl))
            {
                return true;
            }

            i = partEnd - 1;
        }

        return false;
    }

    private static int CompositePartEnd(ReadOnlyArray<InputBinding> bindings, int rootIndex)
    {
        var end = rootIndex + 1;
        while (end < bindings.Count && bindings[end].isPartOfComposite)
        {
            end++;
        }

        return end;
    }

    // A chord is active when every configured part resolves to a pressed control and the triggering
    // control belongs to the chord. Unity can report the modifier that completed a composite as the
    // triggering control, so either part may match as long as the terminal is pressed as well.
    private static bool ChordActive(
        InputAction action,
        ReadOnlyArray<InputBinding> bindings,
        int rootIndex,
        int partEnd,
        InputControl triggeringControl)
    {
        if (partEnd == rootIndex + 1)
        {
            return TerminalMatches(bindings[rootIndex], triggeringControl);
        }

        var triggeringPartFound = false;
        var terminalPressed = false;
        for (var p = rootIndex + 1; p < partEnd; p++)
        {
            var part = bindings[p];
            if (!AnyResolvedControlPressed(action, part.effectivePath))
            {
                return false;
            }

            if (!IsModifierPart(part))
            {
                terminalPressed = true;
            }

            if (TerminalMatches(part, triggeringControl))
            {
                triggeringPartFound = true;
            }
        }

        return triggeringPartFound && terminalPressed;
    }

    // A sibling in the same map whose active chord strictly contains this chord's modifiers keeps its
    // own more specific tweak. Identical chords intentionally stay shared.
    private static bool MoreSpecificSiblingOwns(
        InputAction action,
        int root,
        int partEnd,
        InputControl triggeringControl)
    {
        var map = action.actionMap;
        if (map == null)
        {
            return false;
        }

        var bindings = action.bindings;
        var modifierCount = CountModifierParts(bindings, root + 1, partEnd);
        foreach (var sibling in map.actions)
        {
            if (sibling == action || !sibling.enabled)
            {
                continue;
            }

            var siblingBindings = sibling.bindings;
            for (var i = 0; i < siblingBindings.Count; i++)
            {
                if (siblingBindings[i].isPartOfComposite)
                {
                    continue;
                }

                var siblingPartEnd = CompositePartEnd(siblingBindings, i);
                if (CountModifierParts(siblingBindings, i + 1, siblingPartEnd) > modifierCount
                    && ChordActive(sibling, siblingBindings, i, siblingPartEnd, triggeringControl)
                    && ContainsAllModifiers(action, root + 1, partEnd, sibling, i + 1, siblingPartEnd))
                {
                    return true;
                }

                i = siblingPartEnd - 1;
            }
        }

        return false;
    }

    private static int CountModifierParts(ReadOnlyArray<InputBinding> bindings, int start, int end)
    {
        var count = 0;
        for (var i = start; i < end; i++)
        {
            if (IsModifierPart(bindings[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static bool ContainsAllModifiers(
        InputAction action,
        int start,
        int end,
        InputAction siblingAction,
        int siblingStart,
        int siblingEnd)
    {
        var bindings = action.bindings;
        var siblingBindings = siblingAction.bindings;
        for (var p = start; p < end; p++)
        {
            if (!IsModifierPart(bindings[p]))
            {
                continue;
            }

            var matched = false;
            for (var s = siblingStart; s < siblingEnd && !matched; s++)
            {
                if (IsModifierPart(siblingBindings[s])
                    && ModifierPartsEquivalent(
                        bindings[p], action, siblingBindings[s], siblingAction))
                {
                    matched = true;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ModifierPartsEquivalent(
        InputBinding part,
        InputAction action,
        InputBinding siblingPart,
        InputAction siblingAction)
    {
        if (string.Equals(
            part.effectivePath, siblingPart.effectivePath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return PathsResolveTogether(part.effectivePath, siblingPart.effectivePath, action.controls)
            || PathsResolveTogether(part.effectivePath, siblingPart.effectivePath, siblingAction.controls);
    }

    private static bool PathsResolveTogether(
        string pathA, string pathB, ReadOnlyArray<InputControl> controls)
    {
        for (var i = 0; i < controls.Count; i++)
        {
            var control = controls[i];
            if (InputControlPath.Matches(pathA, control) && InputControlPath.Matches(pathB, control))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsModifierPart(InputBinding part) =>
        part.name != null && part.name.StartsWith("modifier", StringComparison.OrdinalIgnoreCase);

    private static bool TerminalMatches(InputBinding part, InputControl control)
    {
        var path = part.effectivePath;
        return !string.IsNullOrEmpty(path) && InputControlPath.Matches(path, control);
    }

    private static bool AnyResolvedControlPressed(InputAction action, string effectivePath)
    {
        if (string.IsNullOrEmpty(effectivePath))
        {
            return false;
        }

        var controls = action.controls;
        for (var i = 0; i < controls.Count; i++)
        {
            var control = controls[i];
            if (control.IsPressed() && InputControlPath.Matches(effectivePath, control))
            {
                return true;
            }
        }

        return false;
    }
}
