using UnityEngine;
using UnityEngine.UI;

class RefreshLayoutGroup : MonoBehaviour
{
    [SerializeField]
    RectTransform LayoutGroup;

    public void TriggerRefresh()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(LayoutGroup);
    }
}