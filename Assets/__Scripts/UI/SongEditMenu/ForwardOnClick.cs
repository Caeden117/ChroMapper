using UnityEngine;
using UnityEngine.EventSystems;

public class ForwardOnClick : MonoBehaviour, IPointerClickHandler {

    public void OnPointerClick(PointerEventData eventData) {
        var diff = transform.parent.parent;
        var diffSelector = transform.parent.parent.parent.GetComponent<DifficultySelect>();

        if (diffSelector != null) diffSelector.OnClick(diff);
    }

}
