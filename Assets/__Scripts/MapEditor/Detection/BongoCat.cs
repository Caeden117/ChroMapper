using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BongoCat : MonoBehaviour
{
    [SerializeField] public Sprite dLdR;
    [SerializeField] public Sprite dLuR;
    [SerializeField] public Sprite uLdR;
    [SerializeField] public Sprite uLuR;
    public AudioClip bongoCatAudioClip;
    public AudioUtil audioUtil;
    [SerializeField] public bool Larm = false;
    [SerializeField] public bool Rarm = false;

    private float LarmTimeout;
    private float RarmTimeout;

    private SpriteRenderer comp;

    private void Start()
    {
        comp = GetComponent<SpriteRenderer>();
        comp.enabled = Settings.Instance.BongoBoye;
    }

    public void ToggleBongo()
    {
        Debug.Log("Bongo Cat Toggled");
        if (Settings.Instance.BongoBoye)
            PersistentUI.Instance.DisplayMessage("Bongo cat disabled :(", PersistentUI.DisplayMessageType.BOTTOM);
        else
        {
            audioUtil.PlayOneShotSound(bongoCatAudioClip);
            PersistentUI.Instance.DisplayMessage("Bongo cat joins the fight!", PersistentUI.DisplayMessageType.BOTTOM);
        }
        Settings.Instance.BongoBoye = !Settings.Instance.BongoBoye;
        comp.enabled = Settings.Instance.BongoBoye;
    }

    public void triggerArm(BeatmapNote note, NotesContainer container)
    {
        if (!Settings.Instance.BongoBoye) return;
        BeatmapObjectContainer next = container.LoadedContainers.Where(x => x.objectData._time > note._time &&
        (x.objectData as BeatmapNote)._type == note._type).OrderBy(x => x.objectData._time).FirstOrDefault();
        float half = note._type != BeatmapNote.NOTE_TYPE_BOMB ? container.AudioTimeSyncController.GetSecondsFromBeat((next.objectData._time - note._time) / 2f) : 0f; // ignore bombs
        float timer = next ? Mathf.Clamp(half, 0.05f, 0.2f) : 0.125f; // clamp to accommodate sliders and long gaps between notes
        switch (note._type)
        {
            case BeatmapNote.NOTE_TYPE_A:
                Larm = true;
                LarmTimeout = timer;
                break;
            case BeatmapNote.NOTE_TYPE_B:
                Rarm = true;
                RarmTimeout = timer;
                break;
        }
    }

    private void Update()
    {
        LarmTimeout -= Time.deltaTime;
        RarmTimeout -= Time.deltaTime;
        if (LarmTimeout < 0) Larm = false;
        if (RarmTimeout < 0) Rarm = false;

        if (Larm) comp.sprite = Rarm ? dLdR : dLuR;
        else comp.sprite = Rarm ? uLdR : uLuR;
    }
}
