﻿using System.Collections.Generic;
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
    
    private void AttemptPlaceNJSChange(string njsInput, int easingDropdownValu, bool extend)
    {
        if (string.IsNullOrEmpty(njsInput) || string.IsNullOrWhiteSpace(njsInput)) return;
        if (float.TryParse(njsInput, out var relativeNJS))
        {
            queuedData.Easing = MapTMPDropdownValueToEasing(easingDropdownValu);
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

        var easingDropdown = createNJSEventDialogueBox
            .AddComponent<DropdownComponent>()
            .WithLabel("Easing")
            .WithOptions(beatSaberMapFormatEasings);
            // .WithInitialValue(1); // This doesn't seem to change the initial option even though the value has changed
        
        var extendToggle = createNJSEventDialogueBox
            .AddComponent<ToggleComponent>()
            .WithLabel("Use previous NJS event value")
            .WithInitialValue(false);

        createNJSEventDialogueBox.OnQuickSubmit(() => AttemptPlaceNJSChange(njsTextInput.Value, easingDropdown.Value, extendToggle.Value));

        createNJSEventDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");
        createNJSEventDialogueBox.AddFooterButton(() => AttemptPlaceNJSChange(njsTextInput.Value, easingDropdown.Value, extendToggle.Value), "PersistentUI", "ok");

        createNJSEventDialogueBox.Open();
        
        easingDropdown.Value = 1;
    }
    
    // Probably move to easings class at some point
    private List<string> beatSaberMapFormatEasings = new()
    {
        // Not using an enum because Enum.GetNames has unexpected order for negatives
        "None", // -1
        "Linear", // 0
        "InQuad", // 1
        "OutQuad", // etc.
        "InOutQuad",
        "InSine",
        "OutSine",
        "InOutSine",
        "InCubic",
        "OutCubic",
        "InOutCubic",
        "InQuart",
        "OutQuart",
        "InOutQuart",
        "InQuint",
        "OutQuint",
        "InOutQuint",
        "InExpo",
        "OutExpo",
        "InOutExpo",
        "InCirc",
        "OutCirc",
        "InOutCirc",
        "InBack",
        "OutBack",
        "InOutBack",
        "InElastic",
        "OutElastic",
        "InOutElastic",
        "InBounce",
        "OutBounce",
        "InOutBounce", // 30
        "BeatSaberInOutBack", // 100
        "BeatSaberInOutElastic", // 101
        "BeatSaberInOutBounce" // 102
    };
    
    private static int MapTMPDropdownValueToEasing(int dropdownEasing)
    {
        if (dropdownEasing >= 32)
        {
            return dropdownEasing + (100 - 32);
        }
        else
        {
            return dropdownEasing - 1;
        }
    }
}
