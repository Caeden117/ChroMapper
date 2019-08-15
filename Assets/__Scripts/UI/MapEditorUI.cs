using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MapEditorUI : MonoBehaviour {
    
    [SerializeField] private CanvasGroup[] mainUIGroup;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.H) && Input.GetKey(KeyCode.LeftControl)) {
            foreach(CanvasGroup group in mainUIGroup) ToggleUIVisible(group.alpha != 1, group);
        }
    }

    void ToggleUIVisible(bool visible, CanvasGroup group) {
        if (visible) {
            StartCoroutine(FadeCanvasGroup(group, 0, 1, 1));
        } else {
            StartCoroutine(FadeCanvasGroup(group, 1, 0, 1));
            PersistentUI.Instance.DisplayMessage("CTRL+H to toggle UI", PersistentUI.DisplayMessageType.BOTTOM);
        }
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }


    /*
     * Other
     */

    public void SaveButton() {
        if (BeatSaberSongContainer.Instance.map.Save()) {
            PersistentUI.Instance.DisplayMessage("Map Saved!", PersistentUI.DisplayMessageType.CENTER);
        } else {
            PersistentUI.Instance.DisplayMessage("Error Saving Map!", PersistentUI.DisplayMessageType.CENTER);
        }
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
