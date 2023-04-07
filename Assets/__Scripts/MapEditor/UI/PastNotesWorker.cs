using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PastNotesWorker : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [FormerlySerializedAs("notesContainer")][SerializeField] private NoteGridContainer noteGridContainer;
    [SerializeField] private GameObject gridNotePrefab;
    [SerializeField] private BeatmapObjectCallbackController callbackController;
    [SerializeField] private NoteAppearanceSO noteAppearance;
    private readonly float gridSize = 25f;

    private readonly Dictionary<int, Dictionary<GameObject, Image>> instantiatedNotes =
        new Dictionary<int, Dictionary<GameObject, Image>>();

    private readonly Dictionary<int, BaseNote>
        lastByType = new Dictionary<int, BaseNote>(); //Used to improve performance

    private readonly List<BaseObject> lastGroup = new List<BaseObject>();
    private Canvas canvas;

    private Transform notes;
    private float scale;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        scale = Settings.Instance.PastNotesGridScale;
        canvas.enabled = scale != 0f;
        transform.localScale = Vector3.one * (scale + 0.25f);
        if (scale == 0f) return;

        callbackController.NotePassedThreshold += NotePassedThreshold;
        atsc.TimeChanged += OnTimeChanged;

        notes = transform.GetChild(0);
        Settings.NotifyBySettingName("PastNotesGridScale", UpdatePastNotesGridScale);
    }

    private void OnDestroy()
    {
        callbackController.NotePassedThreshold -= NotePassedThreshold;
        atsc.TimeChanged -= OnTimeChanged;
        Settings.ClearSettingNotifications("PastNotesGridScale");
    }

    private void UpdatePastNotesGridScale(object obj)
    {
        scale = (float)obj;
        canvas.enabled = scale != 0f;
        transform.localScale = Vector3.one * (scale + 0.25f);
    }

    private void OnTimeChanged()
    {
        if (atsc.IsPlaying) return;
        var time = 0f;
        lastGroup.Clear();

        foreach (var note in noteGridContainer.LoadedObjects)
        {
            if (time < note.JsonTime && note.JsonTime < atsc.CurrentBeat)
            {
                time = note.JsonTime;
                lastGroup.Clear();
                if (((BaseNote)note).Type != (int)NoteType.Bomb)
                    lastGroup.Add(note);
            }
            else if (time == note.JsonTime && (note as BaseNote).Type != (int)NoteType.Bomb)
            {
                lastGroup.Add(note);
            }
        }

        foreach (var note in lastGroup) NotePassedThreshold(false, 0, note);
    }

    private void NotePassedThreshold(bool natural, int id, BaseObject obj)
    {
        var note = obj as BaseNote;

        if (!instantiatedNotes.ContainsKey(note.Type))
            instantiatedNotes.Add(note.Type, new Dictionary<GameObject, Image>());

        if (lastByType.TryGetValue(note.Type, out var lastInTime) && lastInTime.JsonTime != obj.JsonTime)
        {
            foreach (var child in instantiatedNotes[note.Type])
                child.Key.SetActive(false);
        }

        if (note.Type == (int)NoteType.Bomb) return;

        float gridPosX = note.PosX, gridPosY = note.PosY;

        if (note.CustomCoordinate != null)
        {
            var pos = (Vector2)note.CustomCoordinate;
            gridPosX = pos.x + 2f;
            gridPosY = pos.y;
        }
        else //mapping extensions ew
        {
            if (gridPosX >= 1000) gridPosX = (gridPosX / 1000) - 1f;
            else if (gridPosX <= -1000f) gridPosX = (gridPosX / 1000f) + 1f;

            if (gridPosY >= 1000) gridPosY = (gridPosY / 1000f) - 1f;
            else if (gridPosY <= -1000f) gridPosY = (gridPosY / 1000f) + 1f;
        }

        var position = new Vector3(gridSize * gridPosX, gridSize * gridPosY, 1);

        if (instantiatedNotes[note.Type].Any(x => x.Key.activeSelf && x.Value.transform.localPosition == position))
            // Note already visible
            return;

        GameObject
            g; //Instead of instantiating new objects every frame (Bad on performance), we are instead using a pooled system to use
        Image img; //Already existing notes, and only create ones we need.
        if (instantiatedNotes[note.Type].Any(x => !x.Key.activeSelf))
        {
            g = instantiatedNotes[note.Type].First(x => !x.Key.activeSelf).Key;
            img = instantiatedNotes[note.Type][g];
            g.SetActive(true);
            g.transform.SetSiblingIndex(g.transform.parent.childCount);
            foreach (Transform child in g.transform) child.gameObject.SetActive(true);
        }
        else
        {
            g = Instantiate(gridNotePrefab, notes.transform, true);
            img = g.GetComponent<Image>();
            instantiatedNotes[note.Type].Add(g, img);
        }

        var transform1 = img.transform;
        transform1.localPosition = position;
        var sc = (scale / 10f) + .06f;
        transform1.localScale = new Vector3(sc, sc); //I have to do this because the UI scaling is weird

        //transform1.rotation = o.transform.rotation; //This code breaks when using 360 maps; use local rotation instead.
        transform1.localEulerAngles =
            Vector3.forward *
            NoteContainer.Directionalize(note)
                .z; //Sets the rotation of the image to match the same rotation as the block
        img.color = note.Type == (int)NoteType.Red ? noteAppearance.RedColor : noteAppearance.BlueColor;

        var dotEnabled =
            note.CutDirection == (int)NoteCutDirection.Any; //Checks to see if the Dot is visible on the block

        if (dotEnabled) g.transform.GetChild(0).gameObject.SetActive(false);
        else g.transform.GetChild(1).gameObject.SetActive(false);
        img.enabled = true;

        if (!lastByType.ContainsKey(note.Type))
            lastByType.Add(note.Type, note);
        else
            lastByType[note.Type] = note;
    }
}
