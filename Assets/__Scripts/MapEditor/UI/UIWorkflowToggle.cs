using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class UIWorkflowToggle : MonoBehaviour
{
    [SerializeField] private RectTransform[] workflowGroups;

    public int selectedWorkflowGroup = 0;

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < workflowGroups.Length; i++)
        {
            RectTransform workflow = workflowGroups[i];
            workflow.anchoredPosition = Vector2.Lerp(workflow.anchoredPosition,
                new Vector2(workflow.anchoredPosition.x, i == selectedWorkflowGroup ? 0 : 35), 0.1f);
        }
    }

    public void UpdateWorkflowGroup()
    {
        selectedWorkflowGroup++;
        if (selectedWorkflowGroup >= workflowGroups.Length) selectedWorkflowGroup = 0;
    }
}
