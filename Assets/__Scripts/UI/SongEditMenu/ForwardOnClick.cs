using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     Used to send clicks from a text field within a button
///     This is pretty brittle, but it gets the job done
/// </summary>
public class ForwardOnClick : MonoBehaviour, IPointerClickHandler
{
    private DifficultySelect diffSelector;

    private void Start() => diffSelector = transform.GetComponentInParent<DifficultySelect>();

    public void OnPointerClick(PointerEventData eventData)
    {
        var diff = transform.parent.parent;

        diffSelector.OnClick(diff);
    }
}
