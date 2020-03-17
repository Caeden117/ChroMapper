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
    [SerializeField] public bool Larm;
    [SerializeField] public bool Rarm;

    private float LarmTimeout;
    private float RarmTimeout;

    private SpriteRenderer comp;

    private void Start()
    {
        comp = GetComponent<SpriteRenderer>();
        comp.enabled = Settings.Instance.BongoBoye;
    }

    private void OnMouseDown()
    {
        if (!comp.enabled) return;
        string msg = "Idk what the fuck bongo cat is thinking.";
        switch (transform.lossyScale.x.ToString("F2"))
        {
            case "0.50":
                msg = "Bongo Cat seems happy with hitting notes all day long.";
                break;
            case "0.74":
                msg = "The lack of exercise is starting to get to Bongo Cat.";
                break;
            case "0.99":
                msg = "Bongo Cat's New Years resolution is to hit the gym every week. I'm not sure if this will happen...";
                break;
            case "1.23":
                msg = "Bongo Cat has been diagnosed with the big obese.";
                break;
            case "1.48":
                msg = "Bongo Cat has resorted to stress eating to cope with his condition.";
                break;
            case "1.72":
                msg = "Bongo Cat is now the world record holder for largest chonk.";
                break;
            case "1.96":
                msg = "\"The first step to getting yourself out of a hole is to stop digging\" doesn't really apply to Bongo Cat.";
                break;
            case "2.21":
                msg = "Hope has been lost a long time ago.";
                break;
            default:
                msg = "Look at the size of this absolute unit.";
                break;
        }
        if (!PersistentUI.Instance.DialogBox_IsEnabled)
            PersistentUI.Instance.ShowDialogBox(msg, null, PersistentUI.DialogBoxPresetType.Ok);
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
        //Ignore bombs here to improve performance.
        if (!Settings.Instance.BongoBoye || note._type == BeatmapNote.NOTE_TYPE_BOMB) return;
        BeatmapObjectContainer next = container.LoadedContainers.FirstOrDefault(x => x.objectData._time > note._time &&
        ((BeatmapNote) x.objectData)._type == note._type);
        float timer = 0.125f;
        if (!(next is null))
        {
            float half = container.AudioTimeSyncController.GetSecondsFromBeat((next.objectData._time - note._time) / 2f);
            timer = next ? Mathf.Clamp(half, 0.05f, 0.2f) : 0.125f; // clamp to accommodate sliders and long gaps between notes
        }
        
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
