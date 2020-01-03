using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

public class MapEditorUI : MonoBehaviour {
    
    [SerializeField] private CanvasGroup[] mainUIGroup;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.H) && Input.GetKey(KeyCode.LeftControl)) {
            if (mainUIGroup.First().alpha == 1)
                PersistentUI.Instance.DisplayMessage("CTRL+H to toggle UI", PersistentUI.DisplayMessageType.BOTTOM);
            foreach (CanvasGroup group in mainUIGroup) ToggleUIVisible(group.alpha != 1, group);
        }
    }

    void ToggleUIVisible(bool visible, CanvasGroup group) {
        StartCoroutine(visible ? FadeCanvasGroup(@group, 0, 1, 1) : FadeCanvasGroup(@group, 1, 0, 1));
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }


    /*
     * Other
     */

    public void SaveButton()
    {
        PersistentUI.Instance.DisplayMessage(BeatSaberSongContainer.Instance.map.Save() ? "Map Saved!" : "Error Saving Map!", PersistentUI.DisplayMessageType.CENTER);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float time = 1f) {
        float t = 0;
        while (t < 1) {
            yield return null;
            t += (Time.deltaTime / time);
            if (t > 1) t = 1;
            group.alpha = Mathf.Lerp(start, end, t);
        }
    }

}
