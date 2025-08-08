using __Scripts.UI.SongEditMenu;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
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
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            // Render an icon for each characteristic in editor
            var icons = new List<Image>
            {
                StandardIcon,
                OneSaberIcon,
                NoArrowsIcon,
                ThreeSixtyDegreesIcon,
                NinetyDegreesIcon,
                LegacyIcon,
                LightshowIcon,
                LawlessIcon
            };

            foreach (var image in icons)
            {
                var itemObject = Instantiate(CustomPropertyItemPrefab, listContainer.transform);
                itemObject.hideFlags = HideFlags.HideAndDontSave;

                var item = itemObject.GetComponent<CharacteristicCustomPropertyItem>();
                item.Image.sprite = image.sprite;
            }

            yield break;
        }
#endif

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

        foreach (var (characteristic, image) in characteristicToIcon)
        {
            var item = Instantiate(CustomPropertyItemPrefab, listContainer.transform)
                .GetComponent<CharacteristicCustomPropertyItem>();
            yield return item.Setup(this, characteristic, image.sprite);
            characteristicToCustomPropertyItem[characteristic] = item;

            ReplaceCharacteristicIcon(characteristic);
            ReplaceCharacteristicTooltip(characteristic);
        }
    }

    public void ReplaceCharacteristicIcon(string characteristic)
    {
        if (!characteristicToIcon.ContainsKey(characteristic)) return;
        
        var customPropertyItem = characteristicToCustomPropertyItem[characteristic];
        var characteristicIcon = characteristicToIcon[characteristic];
        
        characteristicIcon.overrideSprite = customPropertyItem.Image.overrideSprite;
    }

    public void ReplaceCharacteristicTooltip(string characteristic)
    {
        if (!characteristicToIcon.ContainsKey(characteristic)) return;
        
        var customPropertyItem = characteristicToCustomPropertyItem[characteristic];
        var characteristicIcon = characteristicToIcon[characteristic];
        
        var tooltip = characteristicIcon.GetComponent<Tooltip>();
        tooltip.TooltipOverride = customPropertyItem.CustomNameField.text;
    }

    public void OpenEditDialog() => EditDialog.SetActive(!EditDialog.activeSelf);

    public void CommitToInfo()
    {
        foreach (var (_, customPropertyItem) in characteristicToCustomPropertyItem)
        {
            customPropertyItem.CommitToInfo();
        }
    }

    public void UndoChanges()
    {
        foreach (var (characteristic, customPropertyItem) in characteristicToCustomPropertyItem)
        {
            customPropertyItem.UndoChanges();
            
            ReplaceCharacteristicTooltip(characteristic);
            StartCoroutine(customPropertyItem.ReplaceCharacteristicIcons());
        }
    }

    public bool IsDirty() => characteristicToCustomPropertyItem.Any(x => x.Value.IsDirty());
}

