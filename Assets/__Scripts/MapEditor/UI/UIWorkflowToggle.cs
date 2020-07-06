using System.Collections;
using UnityEditor;
using UnityEngine;

public class UIWorkflowToggle : MonoBehaviour
{
    [SerializeField] private RectTransform[] workflowGroups;
    [SerializeField] private GridChild spectrogramGridChild;
    [SerializeField] private GridChild spectrogramChunksChild;

    public int SelectedWorkflowGroup { get; private set; } = 0;

    private IEnumerator UpdateGroup(float dest, RectTransform group)
    {
        float og = group.anchoredPosition.y;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            group.anchoredPosition = new Vector2(group.anchoredPosition.x, Mathf.Lerp(og, dest, t));
            og = group.anchoredPosition.y;
            yield return new WaitForEndOfFrame();
        }
        group.anchoredPosition = new Vector2(group.anchoredPosition.x, dest);
    }

    public void UpdateWorkflowGroup()
    {
        SelectedWorkflowGroup++;
        if (SelectedWorkflowGroup >= workflowGroups.Length) SelectedWorkflowGroup = 0;
        for (int i = 0; i < workflowGroups.Length; i++)
            StartCoroutine(UpdateGroup(i == SelectedWorkflowGroup ? 0 : 35, workflowGroups[i]));

        int order = SelectedWorkflowGroup == 0 ? -1 : 3;
        float offset = SelectedWorkflowGroup == 0 ? 3.5f : 2.5f;

        GridOrderController.DeregisterChild(spectrogramChunksChild);
        GridOrderController.DeregisterChild(spectrogramGridChild);
        if (Settings.Instance.WaveformWorkflow)
        {
            spectrogramChunksChild.Order = spectrogramGridChild.Order = order;
            spectrogramGridChild.LocalOffset = new Vector3(offset, 0, 0);
            spectrogramChunksChild.LocalOffset = new Vector3(offset - 2, 0, 0);
        }
        GridOrderController.RegisterChild(spectrogramChunksChild);
        GridOrderController.RegisterChild(spectrogramGridChild);
    }
}
