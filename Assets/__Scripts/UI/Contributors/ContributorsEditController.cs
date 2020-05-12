using System.Collections;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine;
using System;
using System.IO;

public class ContributorsEditController : MonoBehaviour
{
    [SerializeField] private ContributorsController controller;
    [SerializeField] private TextMeshProUGUI contributorName;
    [SerializeField] private TextMeshProUGUI contributorRole;
    [SerializeField] private Image contributorImage;
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private CanvasGroup editGroup;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField roleInput;
    [SerializeField] private TMP_InputField imageInput;

    private ContributorListItem item;

    public void SelectContributorForEditing(ContributorListItem contributorItem)
    {
        item = contributorItem;
        contributorName.text = contributorItem.Contributor.Name;
        contributorRole.text = contributorItem.Contributor.Role;
        editGroup.interactable = true;
        nameInput.text = contributorItem.Contributor.Name;
        roleInput.text = contributorItem.Contributor.Role;
        imageInput.text = contributorItem.Contributor.LocalImageLocation;
        string location = $"{BeatSaberSongContainer.Instance.song.directory}/{item.Contributor.LocalImageLocation}";
        if (File.Exists(location))
        {
            StartCoroutine(LoadImage());
        }
        else
        {
            contributorImage.sprite = fallbackSprite;
        }
    }

    public void ApplyChanges()
    {
        item.Contributor = new MapContributor(nameInput.text, roleInput.text, imageInput.text);
        item.SetContributorData(item.Contributor);
        SelectContributorForEditing(item);
        controller.RefreshContributors();
    }

    private IEnumerator LoadImage()
    {
        string location = $"{BeatSaberSongContainer.Instance.song.directory}/{item.Contributor.LocalImageLocation}";
        UnityWebRequest request = UnityWebRequestTexture.GetTexture($"file:///{Uri.EscapeDataString(location)}");
        yield return request.SendWebRequest();
        Texture2D tex = DownloadHandlerTexture.GetContent(request);
        contributorImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f);
    }
}
