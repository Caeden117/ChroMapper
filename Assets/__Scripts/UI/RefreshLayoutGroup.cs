using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

internal class RefreshLayoutGroup : MonoBehaviour
{
    [FormerlySerializedAs("LayoutGroup")] [SerializeField] private RectTransform layoutGroup;

    public void TriggerRefresh() => LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup);
}
