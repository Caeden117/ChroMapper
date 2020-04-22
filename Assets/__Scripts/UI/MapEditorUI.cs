using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapEditorUI : MonoBehaviour {
    
    [SerializeField] public CanvasGroup[] mainUIGroup;
    [SerializeField] private CanvasScaler[] extraSizeChanges;

    private List<CanvasScaler> _canvasScalers = new List<CanvasScaler>();
    private List<float> _canvasScalersSizes = new List<float>();

    private Dictionary<CanvasGroup, Coroutine> _canvasFadeCoroutines = new Dictionary<CanvasGroup, Coroutine>();

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

    public void ToggleUIVisible(bool visible, CanvasGroup group)
    {
        Coroutine c = StartCoroutine(visible ? FadeCanvasGroup(@group, group.alpha, 1, 1) : FadeCanvasGroup(@group, group.alpha, 0, 1));
        if (_canvasFadeCoroutines.ContainsKey(group)) _canvasFadeCoroutines[group] = c;
        else _canvasFadeCoroutines.Add(group, c);
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
        Coroutine c = null;
        if (_canvasFadeCoroutines.ContainsKey(group)) c = _canvasFadeCoroutines[group];
        if(c != null) StopCoroutine(c);
        float t = 0;
        while (t < 1) {
            t += (Time.deltaTime / time);
            if (t > 1) t = 1;
            group.alpha = Mathf.MoveTowards(start, end, t);
            yield return new WaitForEndOfFrame();
            if(group.alpha == 1f || group.alpha == 0f) break;
        }
    }

}
