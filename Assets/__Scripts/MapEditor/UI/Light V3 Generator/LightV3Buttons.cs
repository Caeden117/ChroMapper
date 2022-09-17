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

    [SerializeField] private LightV3ColorTemplateSaver colorTemplate;
    [SerializeField] private LightV3RotationTemplateSaver rotationTemplate;
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
    }

    private void StoreCurrentPanel(LightV3GeneratorAppearance.LightV3UIPanel obj)
    {
        currentPanel = obj;
        switchText.text = "Switch to " + (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel ? "Rotation" : "Color");
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
        else
        {
            GroupApply<BeatmapLightRotationEvent, LightRotationEventsContainer>(rotationBinder, BeatmapObject.ObjectType.LightRotationEvent);
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
        else
        {
            rotationTemplate.AddObject(rotationBinder.DisplayingData, name);
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
        else
        {
            rotationTemplate.ApplyObject(rotationBinder.ObjectData, button);
            rotationBinder.Dump(rotationBinder.ObjectData);
            rotationBinder.UpdateToPlacement();
        }
    }

    public void DeleteTemplate(Button button)
    {
        if (currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel)
        {
            colorTemplate.RemoveObject(button);
        }
        else
        {
            rotationTemplate.RemoveObject(button);
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
        else
        {
            rotationTemplate.UpdateObject(currentButton, name);
        }
    }
}
