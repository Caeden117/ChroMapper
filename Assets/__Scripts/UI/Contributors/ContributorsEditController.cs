using System.Collections;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine;
using System;
using System.IO;
using static UnityEngine.InputSystem.InputAction;

public class ContributorsEditController : MenuBase
{
    [SerializeField] private TextMeshProUGUI contributorName;
    [SerializeField] private TextMeshProUGUI contributorRole;
    [SerializeField] private Image contributorImage;
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private CanvasGroup editGroup;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField roleInput;
    [SerializeField] private TMP_InputField imageInput;

    private ContributorListItem item;

    protected override GameObject GetDefault()
    {
        return nameInput.gameObject;
    }

    public override void OnLeaveMenu(CallbackContext context)
    {
        SceneTransitionManager.Instance.LoadScene(2);
    }

    public void SelectContributorForEditing(ContributorListItem contributorItem)
    {
        if (item != null)
        {
            item.EndEdit();
        }

        if (contributorItem == null)
        {
            imageInput.text = roleInput.text = nameInput.text = "";
            editGroup.interactable = false;
            return;
        }

        item = contributorItem;
        editGroup.interactable = true;
        nameInput.text = contributorItem.Contributor.Name;
        roleInput.text = contributorItem.Contributor.Role;
        imageInput.text = contributorItem.Contributor.LocalImageLocation;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        contributorName.text = item.Contributor.Name;
        contributorRole.text = item.Contributor.Role;

        string location = Path.Combine(BeatSaberSongContainer.Instance.song.directory, item.Contributor.LocalImageLocation);
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
        item.SetContributorData(nameInput.text, roleInput.text, imageInput.text);
        UpdatePreview();
    }

    private IEnumerator LoadImage()
    {
        string location = Path.Combine(BeatSaberSongContainer.Instance.song.directory, item.Contributor.LocalImageLocation);
        UnityWebRequest request = UnityWebRequestTexture.GetTexture($"file:///{Uri.EscapeDataString(location)}");
        yield return request.SendWebRequest();
        Texture2D tex = DownloadHandlerTexture.GetContent(request);
        contributorImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f);
    }
}
