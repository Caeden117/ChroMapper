using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;

public static class HoverSelectionMutation
{
    public static bool TryApply<T>(
        T hovered,
        Action<T> mutation,
        out List<BaseObject> editedObjects,
        Predicate<T> matchesType = null)
        where T : BaseObject
    {
        editedObjects = null;
        if (!SelectionController.IsObjectSelected(hovered))
            return false;

        var originals = new List<BaseObject>();
        editedObjects = new List<BaseObject>();
        foreach (var selected in SelectionController.SelectedObjects)
        {
            if (selected is not T candidate
                || candidate.ObjectType != hovered.ObjectType
                || !MatchesNoteType(hovered, candidate)
                || (matchesType != null && !matchesType(candidate)))
            {
                continue;
            }

            var edited = BeatmapFactory.Clone(candidate);
            mutation(edited);
            edited.WriteCustom();
            if (edited.HasSameContent(candidate))
                continue;

            originals.Add(candidate);
            editedObjects.Add(edited);
        }

        if (editedObjects.Count > 0)
        {
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedCollectionAction(editedObjects, originals, "Tweaked selected objects."),
                true);
            SelectionController.NotifySelectionChanged();
        }

        return true;
    }

    private static bool MatchesNoteType(BaseObject hovered, BaseObject candidate) =>
        hovered is not BaseNote hoveredNote
        || candidate is not BaseNote candidateNote
        || (hoveredNote.Type == (int)NoteType.Bomb) == (candidateNote.Type == (int)NoteType.Bomb);
}
