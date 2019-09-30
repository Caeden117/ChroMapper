using System.Collections;
using System.Collections.Generic;
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

    public void triggerArm(int type)
    {
        switch (type)
        {
            case BeatmapNote.NOTE_TYPE_A:
                Larm = true;
                LarmTimeout = 0.175f;
                break;
            case BeatmapNote.NOTE_TYPE_B:
                Rarm = true;
                RarmTimeout = 0.175f;
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
