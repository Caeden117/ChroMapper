using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MetaLightV3TemplateSaver<T> : MonoBehaviour
    where T: BeatmapObject
{
    private class SaveItem
    {
        public Button Button;
        public T ObjectData;
    }
    [SerializeField] private Button buttonTemplate;
    [SerializeField] private Transform buttonsParent;
    private List<SaveItem> objectTemplates = new List<SaveItem>();
    [SerializeField] private Scrollbar scrollbar;
    [SerializeField] private LightV3GeneratorAppearance uiGenerator;
    [SerializeField] private LightV3GeneratorAppearance.LightV3UIPanel thisUIPanel;

    // Start is called before the first frame update
    protected void Start()
    {
        uiGenerator.OnToggleUIPanelSwitch += SwitchDisplayingItems;
    }

    protected void OnDestroy()
    {
        uiGenerator.OnToggleUIPanelSwitch -= SwitchDisplayingItems;
    }

    public void AddObject(T objectData, string name)
    {
        var newButton = Instantiate(buttonTemplate, buttonsParent);
        newButton.GetComponent<LightV3TemplateButton>().SetText(name);
        newButton.gameObject.SetActive(true);
        objectTemplates.Add(new SaveItem
        {
            Button = newButton,
            ObjectData = BeatmapObject.GenerateCopy(objectData)
        });
        AdjustScrollbar(objectTemplates);
    }

    public void ApplyObject(T receiver, Button triggeredButton)
    {
        var idx = objectTemplates.FindIndex(x => x.Button == triggeredButton);
        var data = BeatmapObject.GenerateCopy(objectTemplates[idx].ObjectData);
        receiver.Apply(data);
    }

    public void RemoveObject(Button triggeredButton)
    {
        var idx = objectTemplates.FindIndex(x => x.Button == triggeredButton);
        Destroy(objectTemplates[idx].Button.gameObject);
        objectTemplates.RemoveAt(idx);
        AdjustScrollbar(objectTemplates);
    }

    public void UpdateObject(Button triggeredButton, string name)
    {
        var idx = objectTemplates.FindIndex(x => x.Button == triggeredButton);
        objectTemplates[idx].Button.GetComponent<LightV3TemplateButton>().SetText(name);
    }

    private void AdjustScrollbar<TAny>(List<TAny> items)
    {
        if (items.Count < 10) scrollbar.numberOfSteps = 1;
        else scrollbar.numberOfSteps = items.Count - 10 + 1;
    }

    private void SwitchDisplayingItems(LightV3GeneratorAppearance.LightV3UIPanel currentPanel)
    {
        bool isCurrentPanel = currentPanel == thisUIPanel;
        objectTemplates.ForEach(x => x.Button.gameObject.SetActive(isCurrentPanel));
        AdjustScrollbar(objectTemplates);
        scrollbar.value = 1;
    }
}
