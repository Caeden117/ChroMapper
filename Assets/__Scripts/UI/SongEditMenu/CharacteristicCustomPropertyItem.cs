using System.Collections;
using Beatmap.Info;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacteristicCustomPropertyItem : MonoBehaviour
{
    private CharacteristicCustomPropertyController controller;
    private string characteristic;
    
    [SerializeField] public Image Image;
    [SerializeField] public TMP_InputField CustomNameField;
    private string iconImageFileName = "";
    
    private string initialCustomName;
    private string initialImageFileName;
    
    public IEnumerator Setup(CharacteristicCustomPropertyController controller, string characteristic, Sprite originalIconSprite)
    {
        this.controller = controller;
        
        // Setup placeholder text to match the characteristic name
        this.characteristic = characteristic;
        CustomNameField.placeholder.GetComponent<TMP_Text>().text = characteristic;

        // Setup default sprite to be the characteristic icon
        Image.sprite = originalIconSprite;
        
        // Fill in custom characteristic data if present
        var difficultySet = BeatSaberSongContainer.Instance.Info.DifficultySets.Find(x => x.Characteristic == this.characteristic);
        initialCustomName = CustomNameField.text = difficultySet?.CustomCharacteristicLabel ?? "";
        initialImageFileName = iconImageFileName = difficultySet?.CustomCharacteristicIconImageFileName ?? "";

        CustomNameField.onEndEdit.AddListener(_ => controller.ReplaceCharacteristicTooltip(characteristic));

        if (!string.IsNullOrEmpty(initialImageFileName))
        {
            yield return controller.ImageBrowser.LoadImageIntoSprite(initialImageFileName, Image, isOverride: true);
        }
    }

    public void OnDestroy() => CustomNameField.onEndEdit.RemoveAllListeners();

    public bool IsDirty() => CustomNameField.text != initialCustomName || iconImageFileName != initialImageFileName;
    
    public void CommitToInfo()
    {
        var difficultySet = BeatSaberSongContainer.Instance.Info.DifficultySets.Find(x => x.Characteristic == characteristic);
        if (difficultySet == null)
        {
            difficultySet = new InfoDifficultySet { Characteristic = characteristic };
            BeatSaberSongContainer.Instance.Info.DifficultySets.Add(difficultySet);
        }
        
        initialCustomName = difficultySet.CustomCharacteristicLabel = CustomNameField.text;
        initialImageFileName = difficultySet.CustomCharacteristicIconImageFileName = iconImageFileName;
    }

    public void UndoChanges()
    {
        iconImageFileName = initialImageFileName;
        CustomNameField.text = initialCustomName;
    }

    public void Clear()
    {
        CustomNameField.text = null;
        iconImageFileName = "";
        Image.overrideSprite = null;
        
        controller.ReplaceCharacteristicIcon(characteristic);
        controller.ReplaceCharacteristicTooltip(characteristic);
    }

    private void LoadImageCallback(string imageFileName)
    {
        iconImageFileName = imageFileName;
        StartCoroutine( ReplaceCharacteristicIcons());
    }

    public IEnumerator ReplaceCharacteristicIcons()
    {
        if (!string.IsNullOrEmpty(iconImageFileName))
        {
            yield return controller.ImageBrowser.LoadImageIntoSprite(iconImageFileName, Image, isOverride: true);
        }
        else
        {
            Image.overrideSprite = null;
        }
        
        controller.ReplaceCharacteristicIcon(characteristic);
    }

    public void BrowseForImage() => controller.ImageBrowser.BrowseForImage(LoadImageCallback);
}
