using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BongoCat : MonoBehaviour
{
    [SerializeField] public Sprite dLdR;
    [SerializeField] public Sprite dLuR;
    [SerializeField] public Sprite uLdR;
    [SerializeField] public Sprite uLuR;
    [SerializeField] public int Larm;
    [SerializeField] public int Rarm;

    private float LarmTimeout;
    private float RarmTimeout;

    private SpriteRenderer comp;

    private void Start()
    {
        comp = GetComponent<SpriteRenderer>();
        GetComponent<Renderer>().enabled = false;
    }

    public void ToggleBongo()
    {
        Debug.Log("Bongo Cat Toggled");
        switch (GetComponent<Renderer>().enabled)
        {
            case false:
                GetComponent<Renderer>().enabled = true;
                PersistentUI.Instance.DisplayMessage("Bongo cat joins the fight!", PersistentUI.DisplayMessageType.BOTTOM);
                break;
            case true:
                GetComponent<Renderer>().enabled = false;
                PersistentUI.Instance.DisplayMessage("Bongo cat disabled :(", PersistentUI.DisplayMessageType.BOTTOM);
                break;
        }
    }

    public void triggerArm(int type)
    {
        switch (type)
        {
            case BeatmapNote.NOTE_TYPE_A:
                Larm = 1;
                LarmTimeout = 1;
                break;
            case BeatmapNote.NOTE_TYPE_B:
                Rarm = 1;
                RarmTimeout = 1;
                break;
        }
    }

    private void Update()
    {
        LarmTimeout = Mathf.Max(0, LarmTimeout - 0.1f);
        RarmTimeout = Mathf.Max(0, RarmTimeout - 0.1f);
        if (LarmTimeout <= 0.01) Larm = 0;
        if (RarmTimeout <= 0.01) Rarm = 0;

        switch (Larm)
        {
            case 0:
                switch (Rarm)
                {
                    case 0:
                        comp.sprite = uLuR;
                        break;
                    case 1:
                        comp.sprite = uLdR;
                        break;
                }
                break;
            case 1:
                switch (Rarm)
                {
                    case 0:
                        comp.sprite = dLuR;
                        break;
                    case 1:
                        comp.sprite = dLdR;
                        break;
                }
                break;
        }
    }
}
