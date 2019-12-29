using System.Linq;
using System.Collections;
using UnityEngine;

public class UIWorkflowToggle : MonoBehaviour
{
    [SerializeField] private RectTransform[] workflowGroups;
    [SerializeField] private SoftAttachToNoteGrid measureLinesSoftAttach;

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
        measureLinesSoftAttach.AttachedToNoteGrid = SelectedWorkflowGroup == 0;
    }
}
