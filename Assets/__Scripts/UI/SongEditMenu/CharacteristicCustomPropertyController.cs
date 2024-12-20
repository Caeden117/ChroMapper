using System.Collections;
using System.IO;
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
    
    private IEnumerator Start()
    {
        if (BeatSaberSongContainer.Instance.Info == null) yield break;

        yield return ReplaceCharacteristicIcon(StandardIcon, "Standard");
        yield return ReplaceCharacteristicIcon(OneSaberIcon, "OneSaber");
        yield return ReplaceCharacteristicIcon(NoArrowsIcon, "NoArrows");
        yield return ReplaceCharacteristicIcon(ThreeSixtyDegreesIcon, "ThreeSixtyDegrees");
        yield return ReplaceCharacteristicIcon(NinetyDegreesIcon, "NinetyDegrees");
        yield return ReplaceCharacteristicIcon(LegacyIcon, "Legacy");
        yield return ReplaceCharacteristicIcon(LightshowIcon, "Lightshow");
        yield return ReplaceCharacteristicIcon(LawlessIcon, "Lawless");
    }

    private static int ReplaceCharacteristicIcon(Image image, string characteristic)
    {
        var info = BeatSaberSongContainer.Instance.Info;
        var standardCharacteristic = info.DifficultySets.Find(c => c.Characteristic == characteristic);
        if (!string.IsNullOrEmpty(standardCharacteristic?.CustomCharacteristicIconImageFileName))
        {
            var imagePath = Path.Combine(info.Directory, standardCharacteristic.CustomCharacteristicIconImageFileName);
            if (File.Exists(imagePath))
            {
                var texture = new Texture2D(1, 1);
                texture.LoadImage(File.ReadAllBytes(imagePath));
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                image.overrideSprite = sprite;
            }
        }
        
        if (!string.IsNullOrEmpty(standardCharacteristic?.CustomCharacteristicLabel))
        {
            var tooltip = image.GetComponent<Tooltip>();
            tooltip.TooltipOverride = standardCharacteristic.CustomCharacteristicLabel;
        }
        
        return 0;
    }

    public void OpenEditDialog() => EditDialog.SetActive(!EditDialog.activeSelf);
}

