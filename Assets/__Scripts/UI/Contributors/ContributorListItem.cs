using TMPro;
using UnityEngine;

public class ContributorListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private ContributorsController controller;
    [SerializeField] private ContributorsEditController editController;
    public MapContributor Contributor = null;

    public void SetContributorData(MapContributor contributor)
    {
        gameObject.SetActive(contributor != null);
        Contributor = contributor;
        if (contributor is null) return;
        nameText.text = contributor.Name;
    }

    public void DeleteContributor()
    {
        controller.RemoveContributor(Contributor);
    }

    public void SelectContributorForEditing()
    {
        editController.SelectContributorForEditing(this);
    }
}
