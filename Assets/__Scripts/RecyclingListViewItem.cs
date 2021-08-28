using UnityEngine;

/// <summary>
///     You should subclass this to provide fast access to any data you need to populate
///     this item on demand.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RecyclingListViewItem : MonoBehaviour
{
    private RectTransform rectTransform;
    public RecyclingListView ParentList { get; private set; }

    public int CurrentRow { get; private set; }

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }

    private void Awake() => rectTransform = GetComponent<RectTransform>();

    public void NotifyCurrentAssignment(RecyclingListView v, int row)
    {
        ParentList = v;
        CurrentRow = row;
    }
}
