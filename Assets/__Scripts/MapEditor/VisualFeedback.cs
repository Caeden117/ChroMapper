using System.Collections;
using UnityEngine;

public class VisualFeedback : MonoBehaviour {

    [SerializeField] BeatmapObjectCallbackController callbackController;

    [SerializeField] AnimationCurve anim;

    [SerializeField] float scaleFactor = 1f;

    [SerializeField] bool useColours;
    [SerializeField] Color baseColor;
    [SerializeField] Color red;
    [SerializeField] Color blue;

    [SerializeField] Renderer[] planeRends;

    Vector3 startScale;

    private void Start() {
        startScale = transform.localScale;
    }

    private void OnEnable() {
        callbackController.NotePassedThreshold += HandleCallback;
    }

    private void OnDisable() {
        callbackController.NotePassedThreshold -= HandleCallback;
    }

    float lastTime = -1;
    Color color;
    void HandleCallback(bool initial, int index, BeatmapObject objectData) {
        if (objectData._time == lastTime || !DingOnNotePassingGrid.NoteTypeToDing[(objectData as BeatmapNote)._type]) return;
        /*
         * As for why we are not using "initial", it is so notes that are not supposed to ding do not prevent notes at
         * the same time that are supposed to ding from triggering the sound effects.
         */
        BeatmapNote noteData = (BeatmapNote)objectData;
        if (useColours)
        {
            Color c;
            switch (noteData._type)
            {
                case BeatmapNote.NOTE_TYPE_A:
                    c = red;
                    break;
                case BeatmapNote.NOTE_TYPE_B:
                    c = blue;
                    break;
                default: return;
            }
            color = lastTime == objectData._time ? Color.Lerp(color, c, 0.5f) : c;
        }
        if (t <= 0)
        {
            t = 1;
            StartCoroutine(VisualFeedbackAnim());
        }
        else t = 1;
        lastTime = objectData._time;
    }

    float t = 0;

    IEnumerator VisualFeedbackAnim() {
        while (t > 0) {
            float a = anim.Evaluate(Mathf.Clamp01(t));
            UpdateAppearance(a);
            yield return null;
            t -= Time.deltaTime;
        }
        t = 0;
        UpdateAppearance(0);
    }

    void UpdateAppearance(float a) {
        transform.localScale = startScale * (1 + 0.1f * a * scaleFactor);
        if (useColours)
            foreach (Renderer rend in planeRends)
                //rend.material.SetColor("_GridColour", Color.Lerp(baseColor, color, a));
                rend.material.color = Color.Lerp(baseColor, color, a);
    }

}
