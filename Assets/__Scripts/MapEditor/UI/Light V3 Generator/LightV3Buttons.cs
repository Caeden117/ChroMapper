using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class LightV3Buttons : MonoBehaviour
{
    [SerializeField] private LightV3ColorBinder colorBinder;
    [SerializeField] private LightV3RotationBinder rotationBinder;
    [SerializeField] private LightV3TranslationBinder translationBinder;
    [SerializeField] private LightV3ColorTemplateSaver colorTemplate;
    [SerializeField] private LightV3RotationTemplateSaver rotationTemplate;
    [SerializeField] private LightV3TranslationTemplateSaver translationTemplate;
    [SerializeField] private LightV3GeneratorAppearance uiGenerator;


    [SerializeField] private TMP_Text switchText;
    private DialogBox createTemplatedialogBox;
    private TextBoxComponent createTemplateTextBox;
    private LightV3GeneratorAppearance.LightV3UIPanel currentPanel;

    private DialogBox renameTemplatedialogBox;
    private TextBoxComponent renameTemplateTextBox;
    private Button currentButton;
    private void Start()
    {
        createTemplatedialogBox = PersistentUI.Instance
            .CreateNewDialogBox()
            .WithTitle("Create Template Note")
            .DontDestroyOnClose();
        createTemplateTextBox = createTemplatedialogBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("name");

        createTemplatedialogBox.AddFooterButton(null, "PersistentUI", "cancel");
        createTemplatedialogBox.AddFooterButton(() => AddTemplate(createTemplateTextBox.Value), "PersistentUI", "ok");

        renameTemplatedialogBox = PersistentUI.Instance
            .CreateNewDialogBox()
            .WithTitle("Rename Template Note")
            .DontDestroyOnClose();
        renameTemplateTextBox = renameTemplatedialogBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("name");

        renameTemplatedialogBox.AddFooterButton(null, "PersistentUI", "cancel");
        renameTemplatedialogBox.AddFooterButton(() => RenameTemplateCallback(renameTemplateTextBox.Value), "PersistentUI", "ok");

        uiGenerator.OnToggleUIPanelSwitch += StoreCurrentPanel;

        colorTemplate.AddObject(colorBinder.ObjectData, "default");
        rotationTemplate.AddObject(rotationBinder.ObjectData, "default").SetActive(false);
        translationTemplate.AddObject(translationBinder.ObjectData, "default").SetActive(false);
    }

    private void StoreCurrentPanel(LightV3GeneratorAppearance.LightV3UIPanel obj)
    {
        currentPanel = obj;
        switchText.text = "Switch to ";
        switch (currentPanel)
        {
            case LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel:
                switchText.text += "Rotation";
                break;
            case LightV3GeneratorAppearance.LightV3UIPanel.LightRotationPanel:
                switchText.text += uiGenerator.PlatformDescriptor.HasTranslationEvent ? "Translation" : "Color";
                break;
            case LightV3GeneratorAppearance.LightV3UIPanel.LightTranslationPanel:
                switchText.text += "Color";
                break;
        }
    }

    private void OnDestroy()
    {
        uiGenerator.OnToggleUIPanelSwitch -= StoreCurrentPanel;
    }
    public void Apply()
    {
        /*
        if (SelectionController.SelectedObjects.Count == 1)
        {
            var obj = SelectionController.SelectedObjects.First();
            if (obj is BeatmapLightColorEvent color)
            {
                colorBinder.Load(color);
                if (color.HasAttachedContainer)
                {
                    var col = BeatmapObjectContainerCollection.GetCollectionForType<LightColorEventsContainer>(BeatmapObject.ObjectType.LightColorEvent);
                    col.RefreshPool(true);
                }
            }
            if (obj is BeatmapLightRotationEvent rot)
            {
                rotationBinder.Load(rot);
                if (rot.HasAttachedContainer)
                {
                    var col = BeatmapObjectContainerCollection.GetCollectionForType<LightRotationEventsContainer>(BeatmapObject.ObjectType.LightRotationEvent);
                    col.RefreshPool(true);
                }
            }
        }
        */
        if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel)
        {
            GroupApply<BeatmapLightColorEvent, LightColorEventsContainer>(colorBinder, BeatmapObject.ObjectType.LightColorEvent);
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightRotationPanel)
        {
            GroupApply<BeatmapLightRotationEvent, LightRotationEventsContainer>(rotationBinder, BeatmapObject.ObjectType.LightRotationEvent);
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightTranslationPanel)
        {
            GroupApply<BeatmapLightTranslationEvent, LightTranslationEventsContainer>(translationBinder, BeatmapObject.ObjectType.LightTranslationEvent);
        }
    }

    private void GroupApply<T, TBocc>(MetaLightV3Binder<T> binder, BeatmapObject.ObjectType objectType)
        where T : BeatmapObject
        where TBocc: BeatmapObjectContainerCollection
    {
        var allActions = new List<BeatmapAction>(); 
        foreach (var obj in SelectionController.SelectedObjects.OfType<T>())
        {
            var original = BeatmapObject.GenerateCopy(obj);
            binder.Load(obj);
            allActions.Add(new BeatmapObjectModifiedAction(obj, obj, original, "", true));
        }
        var col = BeatmapObjectContainerCollection.GetCollectionForType<TBocc>(objectType);
        col.RefreshPool(true);
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, false, $"apply {objectType} to selected notes"));
    }

    public void GenerateTemplate()
    {
        createTemplatedialogBox.Open();
    }
    
    private void AddTemplate(string name)
    {
        if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel)
        {
            colorTemplate.AddObject(colorBinder.DisplayingData, name);
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightRotationPanel)
        {
            rotationTemplate.AddObject(rotationBinder.DisplayingData, name);
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightTranslationPanel)
        {
            translationTemplate.AddObject(translationBinder.DisplayingData, name);
        }
    }

    public void RestoreTemplate(Button button)
    {
        if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel)
        {
            colorTemplate.ApplyObject(colorBinder.ObjectData, button);
            colorBinder.Dump(colorBinder.ObjectData);
            colorBinder.UpdateToPlacement();
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightRotationPanel)
        {
            rotationTemplate.ApplyObject(rotationBinder.ObjectData, button);
            rotationBinder.Dump(rotationBinder.ObjectData);
            rotationBinder.UpdateToPlacement();
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightTranslationPanel)
        {
            translationTemplate.ApplyObject(translationBinder.ObjectData, button);
            translationBinder.Dump(translationBinder.ObjectData);
            translationBinder.UpdateToPlacement();
        }
    }

    public void DeleteTemplate(Button button)
    {
        if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel)
        {
            colorTemplate.RemoveObject(button);
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightRotationPanel)
        {
            rotationTemplate.RemoveObject(button);
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightTranslationPanel)
        {
            translationTemplate.RemoveObject(button);
        }
    }

    public void RenameTemplate(Button button)
    {
        currentButton = button;
        renameTemplatedialogBox.Open();
    }

    private void RenameTemplateCallback(string name)
    {
        if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel)
        {
            colorTemplate.UpdateObject(currentButton, name);
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightRotationPanel)
        {
            rotationTemplate.UpdateObject(currentButton, name);
        }
        else if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightTranslationPanel)
        {
            translationTemplate.UpdateObject(currentButton, name);
        }
    }

    public void GenerateDefaultEventsAtStart()
    {
        var colorCol = BeatmapObjectContainerCollection.GetCollectionForType<LightColorEventsContainer>(BeatmapObject.ObjectType.LightColorEvent);
        var rotationCol = BeatmapObjectContainerCollection.GetCollectionForType<LightRotationEventsContainer>(BeatmapObject.ObjectType.LightRotationEvent);
        var translationCol = BeatmapObjectContainerCollection.GetCollectionForType<LightTranslationEventsContainer>(BeatmapObject.ObjectType.LightTranslationEvent);
        var platformDescriptor = colorCol.platformDescriptor;
        if (HasEventAtStart(colorCol) || HasEventAtStart(rotationCol) || (platformDescriptor.HasTranslationEvent && HasEventAtStart(translationCol)))
        {
            PersistentUI.Instance.ShowDialogBox("There seems to be some events at time 0. Please remove them first.", null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }
        var allActions = new List<BeatmapAction>();
        for (int i = 0; i < platformDescriptor.LightsManagersV3.Length; ++i)
        {
            var lightManager = platformDescriptor.LightsManagersV3[i];
            if (lightManager.HasColorEvent)
            {
                var obj = new BeatmapLightColorEvent();
                obj.EventBoxes[0].EventDatas[0].Brightness = 0;
                obj.Group = lightManager.GroupId;
                colorCol.SpawnObject(obj, out var conflict, false, false);
                allActions.Add(new BeatmapObjectPlacementAction(obj, conflict, ""));
            }
            if (lightManager.HasRotationEvent)
            {
                for (int axis = 0; axis <= 2; ++axis)
                {
                    if (!lightManager.IsValidRotationAxis(axis)) continue;
                    var obj = new BeatmapLightRotationEvent();
                    obj.Group = lightManager.GroupId;
                    rotationCol.SpawnObject(obj, out var conflict, false, false);
                    allActions.Add(new BeatmapObjectPlacementAction(obj, conflict, ""));
                }
            }
            if (lightManager.HasTranslationEvent)
            {
                for (int axis = 0; axis <= 2; ++axis)
                {
                    if (!lightManager.IsValidTranslationAxis(axis)) continue;
                    var obj = new BeatmapLightTranslationEvent();
                    obj.Group = lightManager.GroupId;
                    translationCol.SpawnObject(obj, out var conflict, false, false);
                    allActions.Add(new BeatmapObjectPlacementAction(obj, conflict, ""));
                }
            }
        }
        colorCol.RefreshPool(true);
        rotationCol.RefreshPool(true);
        if (platformDescriptor.HasTranslationEvent) translationCol.RefreshPool(true);
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, false, $"Spawn default events at start"));
    }

    private bool HasEventAtStart<T>(T col)
        where T: BeatmapObjectContainerCollection
    {
        return col.GetBetween(0, 1e-3f).Any();
    }
}
