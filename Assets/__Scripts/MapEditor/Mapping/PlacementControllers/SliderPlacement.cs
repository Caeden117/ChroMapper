using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class SliderPlacement : PlacementController<BeatmapSlider, BeatmapSliderContainer, SlidersContainer>,
    CMInput.ISliderPlacementActions
{
    private static HashSet<BeatmapObject> SelectedObjects => SelectionController.SelectedObjects;
    

    public void OnSpawnSlider(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) return;
        if (!Settings.Instance.Load_MapV3) return;
        var objects = SelectedObjects.ToList();
        if (objects.Count != 2) { return; }
        if(!IsColorNote(objects[0]) || !IsColorNote(objects[1]))
        {
            return;
        }
        var n1 = objects[0] as BeatmapNote;
        var n2 = objects[1] as BeatmapNote;
        if (n1.Time > n2.Time) { var t = n1; n1 = n2; n2 = t; }
        if (n1.CutDirection == BeatmapNote.NoteCutDirectionAny || n2.CutDirection == BeatmapNote.NoteCutDirectionAny) { return; }
        var sliderData = new BeatmapSlider
        {
            Time = n1.Time,
            Color = n1.Type,
            X = n1.LineIndex,
            Y = n1.LineLayer,
            Direction = n1.CutDirection,
            TailTime = n2.Time,
            TailX = n2.LineIndex,
            TailY = n2.LineLayer,
            HeadControlPointLengthMultiplier = 1.0f,
            TailControlPointLengthMultiplier = 1.0f,
            TailCutDirection = n2.CutDirection,
            SliderMidAnchorMode = 0
        };
        SpawnSlider(sliderData);
    }

    public static bool IsColorNote(BeatmapObject o)
    {
        return o is BeatmapNote && !(o is BeatmapBombNote);
    }

    public override BeatmapSlider GenerateOriginalData() => new BeatmapSlider();
    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a slider.");

    public void SpawnSlider(BeatmapSlider sliderData)
    {
        var sliderContainer = objectContainerCollection;
        sliderContainer.SpawnObject(sliderData, false);
        BeatmapActionContainer.AddAction(GenerateAction(sliderData, new List<BeatmapObject>()));
    }




    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        return;
    }
    public override void TransferQueuedToDraggedObject(ref BeatmapSlider dragged, BeatmapSlider queued) => throw new System.NotImplementedException();

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }
}
