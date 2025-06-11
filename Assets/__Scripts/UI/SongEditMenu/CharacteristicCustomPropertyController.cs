using __Scripts.UI.SongEditMenu;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CharacteristicCustomPropertyController : MonoBehaviour
{
    [SerializeField] private GameObject EditDialog;
    [SerializeField] private Button EditButton;
    
    [SerializeField] private Image StandardIcon;
    [SerializeField] private Image OneSaberIcon;
    [SerializeField] private Image NoArrowsIcon;
    [SerializeField] private Image ThreeSixtyDegreesIcon;
    [SerializeField] private Image NinetyDegreesIcon;
    [SerializeField] private Image LegacyIcon;
    [SerializeField] private Image LightshowIcon;
    [SerializeField] private Image LawlessIcon;

    [SerializeField] private GameObject listContainer;
    [SerializeField] private GameObject CustomPropertyItemPrefab;

    public ImageBrowser ImageBrowser; // Used by items
    
    private Dictionary<string, CharacteristicCustomPropertyItem> characteristicToCustomPropertyItem = new();
    private Dictionary<string, Image> characteristicToIcon = new();

    private IEnumerator Start()
    {
        if (BeatSaberSongContainer.Instance.Info == null) yield break;
        
        characteristicToIcon = new Dictionary<string, Image>
        {
            { "Standard", StandardIcon },
            { "OneSaber", OneSaberIcon },
            { "NoArrows", NoArrowsIcon },
            { "360Degree", ThreeSixtyDegreesIcon },
            { "90Degree", NinetyDegreesIcon },
            { "Legacy", LegacyIcon },
            { "Lightshow", LightshowIcon },
            { "Lawless", LawlessIcon },
        };

        foreach (var characteristicIcon in characteristicToIcon)
        {
            var item = Instantiate(CustomPropertyItemPrefab, listContainer.transform)
                .GetComponent<CharacteristicCustomPropertyItem>();
            yield return item.Setup(this, characteristicIcon.Key, characteristicIcon.Value.sprite);
            characteristicToCustomPropertyItem[characteristicIcon.Key] = item;
            
            ReplaceCharacteristicIcon(characteristicIcon.Key);
        }
    }

    public void ReplaceCharacteristicIcon(string characteristic)
    {
        if (!characteristicToIcon.ContainsKey(characteristic)) return;
        
        var customPropertyItem = characteristicToCustomPropertyItem[characteristic];
        var characteristicIcon = characteristicToIcon[characteristic];
        
        characteristicIcon.overrideSprite = customPropertyItem.Image.overrideSprite;
        
        var tooltip = characteristicIcon.GetComponent<Tooltip>();
        tooltip.TooltipOverride = customPropertyItem.CustomNameField.text;
    }

    public void OpenEditDialog() => EditDialog.SetActive(!EditDialog.activeSelf);

    public void CommitToInfo()
    {
        foreach (var characteristicCustomPropertyItem in characteristicToCustomPropertyItem)
        {
            characteristicCustomPropertyItem.Value.CommitToInfo();
        }
    }

    public bool IsDirty() => characteristicToCustomPropertyItem.Any(x => x.Value.IsDirty());
}

