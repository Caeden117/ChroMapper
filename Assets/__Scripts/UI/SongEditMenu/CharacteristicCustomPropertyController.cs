using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
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
            item.Setup(this, characteristicIcon.Key, characteristicIcon.Value.sprite);
            characteristicToCustomPropertyItem[characteristicIcon.Key] = item;
            
            yield return CleanLoad(characteristicIcon.Key);
            ReplaceCharacteristicIcon(characteristicIcon.Key);
        }
    }

    private IEnumerator CleanLoad(string characteristic)
    {
        var customPropertyItem = characteristicToCustomPropertyItem[characteristic];
        var customImageFileName = customPropertyItem.iconImageFileName;
        if (string.IsNullOrEmpty(customImageFileName))
        {
            customPropertyItem.Image.overrideSprite = null;
        }
        else
        {
            var location = Path.Combine(BeatSaberSongContainer.Instance.Info.Directory, customImageFileName);

            var uriPath = Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                ? Uri.EscapeDataString(location)
                : Uri.EscapeUriString(location);
        
            var request = UnityWebRequestTexture.GetTexture($"file:///{uriPath}");
        
            yield return request.SendWebRequest();
        
            var tex = DownloadHandlerTexture.GetContent(request);
            customPropertyItem.Image.overrideSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f);
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

