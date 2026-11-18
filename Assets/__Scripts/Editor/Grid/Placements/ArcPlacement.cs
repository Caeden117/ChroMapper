using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine.InputSystem;

public class ArcPlacement : BasePlacement<BaseArc, ArcContainer, ArcGridContainer>,
                            CMInput.IArcPlacementActions
{
    private static HashSet<BaseObject> SelectedObjects => SelectionController.SelectedObjects;

    // Read arc defaults in Awake; Settings.Instance cannot be initialized from a MonoBehaviour constructor.
    [NonSerialized] public float HeadMultiplier;
    [NonSerialized] public float TailMultiplier;

    private void Awake()
    {
        HeadMultiplier = Settings.Instance.DefaultArcHeadMultiplier;
        TailMultiplier = Settings.Instance.DefaultArcTailMultiplier;
    }

    public void OnSpawnArc(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) return;

        SpawnArcsFromSelection();
    }

    public int SpawnArcsFromSelection()
    {
        var notes = SelectedObjects.Where(IsColorNote).Cast<BaseNote>().ToList();
        notes.Sort((a, b) => a.JsonTime.CompareTo(b.JsonTime));

        if (Settings.Instance.MapVersion == 2 && notes.Count > 1)
        {
            PersistentUI.Instance.ShowDialogBox(
                "Arc placement is not supported in v2 format.\nConvert map to v3 to place arcs.",
                null,
                PersistentUI.DialogBoxPresetType.Ok);
            return 0;
        }

        // is there better way than this?
        var generatedObjects = new List<BaseArc>();
        var red = notes.Where(n => n.Color == (int)NoteColor.Red).ToList();
        var blue = notes.Where(n => n.Color == (int)NoteColor.Blue).ToList();

        for (var i = 1; i < red.Count; i++) generatedObjects.Add(CreateArcData(red[i - 1], red[i]));

        for (var i = 1; i < blue.Count; i++) generatedObjects.Add(CreateArcData(blue[i - 1], blue[i]));

        if (generatedObjects.Count > 0)
        {
            foreach (var arcData in generatedObjects) ObjectContainerCollection.SpawnObject(arcData, false);

            SelectionController.DeselectAll();
            SelectionController.SelectedObjects = new HashSet<BaseObject>(generatedObjects);
            SelectionController.NotifySelectionChanged();
            SelectionController.RefreshSelectionMaterial(false);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    generatedObjects.ToArray(),
                    new List<BaseObject>(),
                    $"Placed {generatedObjects.Count} arcs"));
        }

        return generatedObjects.Count;
    }

    public static bool IsColorNote(BaseObject o) => o is BaseNote note && note.Type != (int)NoteType.Bomb;

    protected override BaseArc GenerateOriginalData() => new();

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed an arc.");

    public BaseArc CreateArcData(BaseNote head, BaseNote tail)
    {
        if (head.JsonTime > tail.JsonTime) (head, tail) = (tail, head);

        return new BaseArc(head, tail)
        {
            HeadControlPointLengthMultiplier = HeadMultiplier, TailControlPointLengthMultiplier = TailMultiplier
        };
    }
}
