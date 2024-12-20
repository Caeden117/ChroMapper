using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public class NJSEventPlacement : PlacementController<BaseNJSEvent, NJSEventContainer, NJSEventGridContainer>
{
    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a NJS Event at time {spawned.JsonTime}");

    public override BaseNJSEvent GenerateOriginalData() => new();

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __) =>
        instantiatedContainer.transform.localPosition =
            new Vector3(0.5f, 0.5f, instantiatedContainer.transform.localPosition.z);

    public override void TransferQueuedToDraggedObject(ref BaseNJSEvent dragged, BaseNJSEvent queued) =>
        dragged.SetTimes(queued.JsonTime, queued.SongBpmTime);
    
    internal override void ApplyToMap() => CreateAndOpenNJSDialogue(isInitialPlacement: true);
    
    private void AttemptPlaceNJSChange(string njsInput, string easingInput, bool extend)
    {
        if (string.IsNullOrEmpty(njsInput) || string.IsNullOrWhiteSpace(njsInput)) return;
        if (float.TryParse(njsInput, out var relativeNJS) && int.TryParse(easingInput, out var easing))
        {
            queuedData.Easing = easing;
            queuedData.RelativeNJS = relativeNJS;
            queuedData.UsePrevious = extend ? 1 : 0;
            base.ApplyToMap();
        }
        else
        {
            CreateAndOpenNJSDialogue(isInitialPlacement: false);
        }
    }

    private void CreateAndOpenNJSDialogue(bool isInitialPlacement)
    {
        // TODO: Why aren't we caching this dialogue box? Two bugs:
        //    1) The footer buttons can trigger off the same click that opens this dialogue which causes an instant close
        //    2) Immediately reopening the dialogue box after closing it doesn't work

        var createNJSEventDialogueBox = PersistentUI.Instance
            .CreateNewDialogBox()
            .WithTitle("[WIP] Please enter the NJS and easing for this NJS event");
            // .WithTitle("Mapper", "NJS.dialog");

        if (!isInitialPlacement)
        {
            createNJSEventDialogueBox
                .AddComponent<TextComponent>()
                .WithInitialValue("Mapper", "NJS.dialogue.invalidnumber");
        }

        var njsTextInput = createNJSEventDialogueBox
            .AddComponent<TextBoxComponent>()
            .WithLabel($"Relative NJS")
            .WithInitialValue(0.ToString());

        // TODO: Change into easing dropdown
        var easingInput = createNJSEventDialogueBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("Easing")
            .WithInitialValue((-1).ToString());
        
        
        var extendToggle = createNJSEventDialogueBox
            .AddComponent<ToggleComponent>()
            .WithLabel("Use previous NJS event value")
            .WithInitialValue(false);

        createNJSEventDialogueBox.OnQuickSubmit(() => AttemptPlaceNJSChange(njsTextInput.Value, easingInput.Value, extendToggle.Value));

        createNJSEventDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");
        createNJSEventDialogueBox.AddFooterButton(() => AttemptPlaceNJSChange(njsTextInput.Value, easingInput.Value, extendToggle.Value), "PersistentUI", "ok");

        createNJSEventDialogueBox.Open();
    }
}
