using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MapEditorUI : MonoBehaviour
{
    [FormerlySerializedAs("mainUIGroup")] public CanvasGroup[] MainUIGroup;
    [SerializeField] private CanvasScaler[] extraSizeChanges;

    private readonly Dictionary<CanvasGroup, Coroutine>
        canvasFadeCoroutines = new Dictionary<CanvasGroup, Coroutine>();

    private readonly List<CanvasScaler> canvasScalers = new List<CanvasScaler>();
    private readonly List<float> canvasScalersSizes = new List<float>();

    private void Start()
    {
        foreach (var group in MainUIGroup)
        {
            var cs = group.transform.parent.GetComponent<CanvasScaler>();
            if (cs != null)
            {
                canvasScalers.Add(cs);
                canvasScalersSizes.Add(cs.referenceResolution.x);
            }
        }

        foreach (var cs in extraSizeChanges)
        {
            canvasScalers.Add(cs);
            canvasScalersSizes.Add(cs.referenceResolution.x);
        }
    }

    public void ToggleUIVisible(bool visible, CanvasGroup group)
    {
        var c = StartCoroutine(
            visible ? FadeCanvasGroup(group, group.alpha, 1) : FadeCanvasGroup(group, group.alpha, 0));
        if (canvasFadeCoroutines.ContainsKey(group)) canvasFadeCoroutines[group] = c;
        else canvasFadeCoroutines.Add(group, c);
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float time = 1f)
    {
        Coroutine c = null;
        if (canvasFadeCoroutines.ContainsKey(group)) c = canvasFadeCoroutines[group];
        if (c != null) StopCoroutine(c);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            if (t > 1) t = 1;
            group.alpha = Mathf.MoveTowards(start, end, t);
            yield return new WaitForEndOfFrame();
            if (group.alpha == 1f || group.alpha == 0f) break;
        }
    }
}
