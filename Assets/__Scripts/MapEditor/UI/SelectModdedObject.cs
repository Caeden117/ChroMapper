using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

// TODO: Localise strings
public class SelectModdedObject : MonoBehaviour
{
    private DialogBox selectModdedObjectDialogueBox;
    private DropdownComponent dropdownComponent;

    private int checker;
    
    private void Start()
    {
        var title = "Choose what modded data to select.";

        var options = new List<string>
        {
            "Any (NE or ME)",
            "Noodle Extensions",
            "Mapping Extensions"
        };
        
        // Create and cache dialog box for later use
        selectModdedObjectDialogueBox = PersistentUI.Instance
            .CreateNewDialogBox()
            .WithTitle(title)
            .DontDestroyOnClose();
        
        dropdownComponent = selectModdedObjectDialogueBox
            .AddComponent<DropdownComponent>()
            .WithLabel("Mod requirement")
            .WithOptions(options);
        
        selectModdedObjectDialogueBox
            .AddComponent<TextComponent>()
            .WithInitialValue("Note: does not search for track data.");
        
        // Enable quick submit
        selectModdedObjectDialogueBox.OnQuickSubmit(DoTheThing);

        // Cancel button
        selectModdedObjectDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");

        // Submit/OK button
        selectModdedObjectDialogueBox.AddFooterButton(DoTheThing, "PersistentUI", "ok");
    }
    
    public void OpenDialogue() => selectModdedObjectDialogueBox.Open();

    private void DoTheThing()
    {
        // SelectionController.Select();
        var noteGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
        var obstacleGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);
        var arcGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        var chainGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        // var eventGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Event);

        var checkForNoodleExtensions = dropdownComponent.Value is 0 or 1;
        var checkForMappingExtensions = dropdownComponent.Value is 0 or 2;
        // var checkForChroma = true;

        var moddedObjects = new HashSet<BaseObject>();
        if (checkForNoodleExtensions)
        {
            moddedObjects.UnionWith(noteGridContainer.MapObjects.Where(n => n.IsNoodleExtensions()));
            moddedObjects.UnionWith(obstacleGridContainer.MapObjects.Where(o => o.IsNoodleExtensions()));
            moddedObjects.UnionWith(arcGridContainer.MapObjects.Where(a => a.IsNoodleExtensions()));
            moddedObjects.UnionWith(chainGridContainer.MapObjects.Where(c => c.IsNoodleExtensions()));
        }

        if (checkForMappingExtensions)
        {
            moddedObjects.UnionWith(noteGridContainer.MapObjects.Where(n => n.IsMappingExtensions()));
            moddedObjects.UnionWith(obstacleGridContainer.MapObjects.Where(o => o.IsMappingExtensions()));
            moddedObjects.UnionWith(arcGridContainer.MapObjects.Where(a => a.IsMappingExtensions()));
            moddedObjects.UnionWith(chainGridContainer.MapObjects.Where(c => c.IsMappingExtensions()));
        }

        if (moddedObjects.Count == 0)
        {
            PersistentUI.Instance.ShowDialogBox("0 results", null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }
        
        SelectionController.SelectedObjects = moddedObjects;
        SelectionController.RefreshSelectionMaterial();
        SelectionController.SelectionChangedEvent?.Invoke();

        var stringBuilder = new StringBuilder();
        
        stringBuilder.AppendLine($"{SelectionController.SelectedObjects.Count} results");
        
        stringBuilder.AppendLine("The first 20 result times are:");
        stringBuilder.AppendLine(string.Join(", ", moddedObjects.Take(20).Select(x => x.JsonTime.ToString("F3"))));

        var message = stringBuilder.ToString();
        PersistentUI.Instance.ShowDialogBox(message, null, PersistentUI.DialogBoxPresetType.Ok);
    }
}
