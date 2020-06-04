using System.Collections;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.EventSystems;
using static UnityEngine.InputSystem.InputAction;

public class ContributorsEditController : MonoBehaviour, CMInput.IMenusExtendedActions
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

    public void OnTab(CallbackContext context)
    {
        if (!context.performed) return;

        var system = EventSystem.current;
        try
        {
            Selectable selected = system.currentSelectedGameObject.GetComponent<Selectable>();

            Selectable next;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                next = selected.FindSelectableOnUp();
            }
            else
            {
                next = selected.FindSelectableOnDown();
            }

            if (next != null)
            {
                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield != null)
                    inputfield.OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret

                system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
            }
        }
        catch (Exception)
        {
            // If there's an error select the default selectable
            system.SetSelectedGameObject(nameInput.gameObject, new BaseEventData(system));
        }
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
