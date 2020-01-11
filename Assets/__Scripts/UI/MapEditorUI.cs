using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapEditorUI : MonoBehaviour {
    
    [SerializeField] private CanvasGroup[] mainUIGroup;
    [SerializeField] private CanvasScaler[] extraSizeChanges;
    [SerializeField] private float aaa;

    private List<CanvasScaler> _canvasScalers = new List<CanvasScaler>();
    private List<float> _canvasScalersSizes = new List<float>();

    private void Start()
    {
        foreach (CanvasGroup group in mainUIGroup)
        {
            CanvasScaler cs = group.transform.parent.GetComponent<CanvasScaler>();
            if (cs != null)
            {
                _canvasScalers.Add(cs);
                _canvasScalersSizes.Add(cs.referenceResolution.x);
            }
        }

        foreach (CanvasScaler cs in extraSizeChanges)
        {
            _canvasScalers.Add(cs);
            _canvasScalersSizes.Add(cs.referenceResolution.x);
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.H) && Input.GetKey(KeyCode.LeftControl)) {
            if (mainUIGroup.First().alpha == 1)
                PersistentUI.Instance.DisplayMessage("CTRL+H to toggle UI", PersistentUI.DisplayMessageType.BOTTOM);
            foreach (CanvasGroup group in mainUIGroup) ToggleUIVisible(group.alpha != 1, group);
        }

        for (int i = 0; i<_canvasScalers.Count; i++)
        {
            CanvasScaler cs = _canvasScalers[i];
            Vector2 scale = cs.referenceResolution;
            scale.x = _canvasScalersSizes[i] * aaa;
            cs.referenceResolution = scale;
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
