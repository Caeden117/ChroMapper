using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Used to send clicks from a text field within a button
/// This is pretty brittle, but it gets the job done
/// </summary>
public class ForwardOnClick : MonoBehaviour, IPointerClickHandler {

    public void OnPointerClick(PointerEventData eventData) {
        var diff = transform.parent.parent;
        var diffSelector = transform.parent.parent.parent.GetComponent<DifficultySelect>();

        diffSelector?.OnClick(diff);
    }

}
