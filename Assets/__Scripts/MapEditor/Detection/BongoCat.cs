using UnityEngine;
using UnityEngine.Serialization;

public class BongoCat : MonoBehaviour
{
    [FormerlySerializedAs("bongoCatAudioClip")] public AudioClip BongoCatAudioClip;
    [FormerlySerializedAs("audioUtil")] public AudioUtil AudioUtil;

    [SerializeField] private Sprite dLdR;
    [SerializeField] private Sprite dLuR;
    [SerializeField] private Sprite uLdR;
    [SerializeField] private Sprite uLuR;
    [FormerlySerializedAs("Larm")] [SerializeField] private bool larm;
    [FormerlySerializedAs("Rarm")] [SerializeField] private bool rarm;

    private SpriteRenderer comp;

    private float larmTimeout;
    private float rarmTimeout;

    private void Start()
    {
        comp = GetComponent<SpriteRenderer>();
        comp.enabled = Settings.Instance.BongoBoye;
    }

    private void Update()
    {
        larmTimeout -= Time.deltaTime;
        rarmTimeout -= Time.deltaTime;
        if (larmTimeout < 0) larm = false;
        if (rarmTimeout < 0) rarm = false;

        if (larm) comp.sprite = rarm ? dLdR : dLuR;
        else comp.sprite = rarm ? uLdR : uLuR;
    }

    public void ToggleBongo()
    {
        Debug.Log("Bongo Cat Toggled");
        if (Settings.Instance.BongoBoye)
        {
            PersistentUI.Instance.DisplayMessage("Bongo cat disabled :(", PersistentUI.DisplayMessageType.Bottom);
        }
        else
        {
            AudioUtil.PlayOneShotSound(BongoCatAudioClip);
            PersistentUI.Instance.DisplayMessage("Bongo cat joins the fight!", PersistentUI.DisplayMessageType.Bottom);
        }

        Settings.Instance.BongoBoye = !Settings.Instance.BongoBoye;
        comp.enabled = Settings.Instance.BongoBoye;
    }

    public void TriggerArm(BeatmapNote note, NotesContainer container)
    {
        //Ignore bombs here to improve performance.
        if (!Settings.Instance.BongoBoye || note.Type == BeatmapNote.NoteTypeBomb) return;
        var next = container.UnsortedObjects.Find(x => x.Time > note.Time &&
                                                       ((BeatmapNote)x).Type == note.Type);
        var timer = 0.125f;
        if (!(next is null))
        {
            var half = container.AudioTimeSyncController.GetSecondsFromBeat((next.Time - note.Time) / 2f);
            timer = next != null
                ? Mathf.Clamp(half, 0.05f, 0.2f)
                : 0.125f; // clamp to accommodate sliders and long gaps between notes
        }

        switch (note.Type)
        {
            case BeatmapNote.NoteTypeA:
                larm = true;
                larmTimeout = timer;
                break;
            case BeatmapNote.NoteTypeB:
                rarm = true;
                rarmTimeout = timer;
                break;
        }
    }
}
