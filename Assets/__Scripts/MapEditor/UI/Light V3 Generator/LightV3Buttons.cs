using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class LightV3Buttons : MonoBehaviour
{
    [SerializeField] private LightV3ColorBinder colorBinder;
    [SerializeField] private LightV3RotationBinder rotationBinder;

    [SerializeField] private LightV3ColorTemplateSaver colorTemplate;
    private DialogBox createTemplatedialogBox;
    private TextBoxComponent createTemplateTextBox;

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

    }

    public void Apply()
    {
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
    }

    public void GenerateTemplate()
    {
        createTemplatedialogBox.Open();
    }
    
    private void AddTemplate(string name)
    {
        colorTemplate.AddObject(colorBinder.DisplayingData, name);
    }

    public void RestoreTemplate(Button button)
    {
        colorTemplate.ApplyObject(colorBinder.ObjectData, button);
        colorBinder.Dump(colorBinder.ObjectData);
        colorBinder.UpdateToPlacement();
    }

    public void DeleteTemplate(Button button)
    {
        colorTemplate.RemoveObject(button);
    }

    public void RenameTemplate(Button button)
    {
        currentButton = button;
        renameTemplatedialogBox.Open();
    }

    private void RenameTemplateCallback(string name)
    {
        colorTemplate.UpdateObject(currentButton, name);
    }
}
