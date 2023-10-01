using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class VisualFeedback : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BeatmapObjectCallbackController callbackController;

    [SerializeField] private AnimationCurve anim;

    [SerializeField] private float scaleFactor = 1f;

    [SerializeField] private bool useColours;
    [SerializeField] private Color baseColor;
    [SerializeField] private Color red;
    [SerializeField] private Color blue;

    [SerializeField] private Renderer[] planeRends;
    private Color color;

    private float lastTime = -1;
    private Vector3 startScale;

    private float t;

    private void Start()
    {
        startScale = transform.localScale;
        atsc.PlayToggle += OnPlayToggle;
    }

    private void OnEnable() => callbackController.NotePassedThreshold += HandleCallback;

    private void OnDisable() => callbackController.NotePassedThreshold -= HandleCallback;

    private void OnDestroy() => atsc.PlayToggle -= OnPlayToggle;

    private void OnPlayToggle(bool playing) => lastTime = -1;

    private void HandleCallback(bool initial, int index, BaseObject objectData)
    {
        if (objectData.JsonTime == lastTime ||
            !DingOnNotePassingGrid.NoteTypeToDing[(objectData as BaseNote).Type])
        {
            return;
        }
        /*
* As for why we are not using "initial", it is so notes that are not supposed to ding do not prevent notes at
* the same time that are supposed to ding from triggering the sound effects.
*/
        var noteData = (BaseNote)objectData;
        if (useColours)
        {
            Color c;
            switch (noteData.Type)
            {
                case (int)NoteType.Red:
                    c = red;
                    break;
                case (int)NoteType.Blue:
                    c = blue;
                    break;
                default: return;
            }

            color = lastTime == objectData.JsonTime ? Color.Lerp(color, c, 0.5f) : c;
        }

        if (t <= 0)
        {
            t = 1;
            StartCoroutine(VisualFeedbackAnim());
        }
        else
        {
            t = 1;
        }

        lastTime = objectData.JsonTime;
    }

    private IEnumerator VisualFeedbackAnim()
    {
        while (t > 0)
        {
            var a = anim.Evaluate(Mathf.Clamp01(t));
            UpdateAppearance(a);
            yield return null;
            t -= Time.deltaTime;
        }

        t = 0;
        UpdateAppearance(0);
    }

    private void UpdateAppearance(float a)
    {
        transform.localScale = startScale * (1 + (0.1f * a * scaleFactor));
        if (useColours)
        {
            foreach (var rend in planeRends)
                //rend.material.SetColor("_GridColour", Color.Lerp(baseColor, color, a));
                rend.material.color = Color.Lerp(baseColor, color, a);
        }
    }
}
