using TMPro;
using UnityEngine;

public class ContributorListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject selectedBg;
    private ContributorsController controller;
    private ContributorsEditController editController;
    public MapContributor Contributor = null;

    public void Setup(MapContributor contributor, ContributorsController controller, ContributorsEditController editController)
    {
        Contributor = contributor;
        this.controller = controller;
        this.editController = editController;

        UpdateName();
    }

    public void SetContributorData(string name, string role, string location)
    {
        Contributor.Name = name;
        Contributor.Role = role;
        Contributor.LocalImageLocation = location;

        UpdateName();
    }

    private void UpdateName()
    {
        nameText.text = Contributor.Name;
    }

    public void DeleteContributor()
    {
        controller.RemoveContributor(this);
    }

    public void SelectContributorForEditing()
    {
        editController.SelectContributorForEditing(this);
        selectedBg.SetActive(true);
    }

    public void EndEdit()
    {
        selectedBg.SetActive(false);
    }
}
