using Beatmap.Info;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContributorListItem : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameText;
    [SerializeField] private TMP_InputField roleText;
    [SerializeField] private Image contributorImage;
    public BaseContributor Contributor;
    private bool dirty;
    private ContributorsController controller;
    private string imagePath = "";

    public bool Dirty
    {
        get => dirty || nameText.text != Contributor.Name || Contributor.Role != roleText.text ||
               Contributor.LocalImageLocation != imagePath;
        set => dirty = value;
    }

    public void Awake() => CheckLoadImage();

    public void Setup(BaseContributor contributor, ContributorsController contributorsControllerNew, bool dirty = false)
    {
        Contributor = contributor;
        controller = contributorsControllerNew;
        this.dirty = dirty;

        nameText.text = Contributor.Name;
        roleText.text = Contributor.Role;
        imagePath = Contributor.LocalImageLocation;

        if (gameObject.activeInHierarchy)
            CheckLoadImage();

        UpdateName();
    }

    private void CheckLoadImage()
    {
        if (!string.IsNullOrWhiteSpace(imagePath))
            SetImageLocation(imagePath);
    }

    private void UpdateName() => nameText.text = Contributor.Name;

    public void Commit()
    {
        Contributor.Name = nameText.text;
        Contributor.Role = roleText.text;
        Contributor.LocalImageLocation = imagePath;
        Dirty = false;
    }

    public void BrowseForImage() => controller.ImageBrowser.BrowseForImage(SetImageLocation);

    private void SetImageLocation(string path)
    {
        imagePath = path;
        StartCoroutine(controller.ImageBrowser.LoadImageIntoSprite(imagePath, contributorImage, isOverride: false));
    }

    public void Delete() => controller.RemoveContributor(this);
}
