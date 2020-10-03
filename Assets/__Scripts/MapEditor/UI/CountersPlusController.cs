using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.Localization.Components;

public class CountersPlusController : MonoBehaviour {

    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private BPMChangesContainer bpm;
    [SerializeField] private AudioSource cameraAudioSource;
    [SerializeField] private AudioTimeSyncController atsc;

    [SerializeField] private LocalizeStringEvent notesMesh;
    [SerializeField] private LocalizeStringEvent notesPSMesh;
    [SerializeField] private LocalizeStringEvent[] extraStrings;

    [SerializeField] private LocalizeStringEvent selectionString;

    private SwingsPerSecond swingsPerSecond;

    private void Start()
    {
        Settings.NotifyBySettingName("CountersPlus", ToggleCounters);
        ToggleCounters(Settings.Instance.CountersPlus);
        StartCoroutine(DelayedUpdate());

        swingsPerSecond = new SwingsPerSecond(notes, obstacles);
        StartCoroutine(CalculateSPS());
    }

    private IEnumerator CalculateSPS()
    {
        while (true)
        {
            yield return new WaitForSeconds(5);
            // Takes ~1ms, calculates red, blue and total stats (we only show total for now)
            swingsPerSecond.Update();
        }
    }

    private IEnumerator DelayedUpdate () {
        while (true)
        {
            yield return new WaitForSeconds(1); //I wouldn't want to update this every single frame.

            if (!Settings.Instance.CountersPlus)
                continue;

            if (SelectionController.HasSelectedObjects() && NotesSelected > 0) {
                notesMesh.StringReference.TableEntryReference = "countersplus.notes.selected";
                notesPSMesh.StringReference.TableEntryReference = "countersplus.nps.selected";
            }
            else
            {
                notesMesh.StringReference.TableEntryReference = "countersplus.notes";
                notesPSMesh.StringReference.TableEntryReference = "countersplus.nps";
            }

            float timeMapping = BeatSaberSongContainer.Instance.map._time;
            seconds = Mathf.Abs(Mathf.FloorToInt(timeMapping * 60 % 60));
            minutes = Mathf.FloorToInt(timeMapping % 60);
            hours = Mathf.FloorToInt(timeMapping / 60);

            notesMesh.StringReference.RefreshString();
            notesPSMesh.StringReference.RefreshString();

            foreach (var str in extraStrings)
            {
                str.StringReference.RefreshString();
            }
        }
	}

    private void Update() // i do want to update this every single frame
    {
        if (Application.isFocused) BeatSaberSongContainer.Instance.map._time += Time.deltaTime / 60; // only tick while application is focused

        selectionString.gameObject.SetActive(SelectionController.HasSelectedObjects());
        if (SelectionController.HasSelectedObjects()) // selected counter; does not rely on counters+ option
        {
            selectionString.StringReference.RefreshString();
        }
    }

    public void ToggleCounters(object value)
    {
        bool enabled = (bool)value;
        foreach (Transform child in transform) child.gameObject.SetActive(enabled);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("CountersPlus");
    }

    ///// Localization /////

    public int NotesCount
    {
        get
        {
            return notes.LoadedObjects.Count;
        }
    }

    public float NPSCount
    {
        get
        {
            return NotesCount / cameraAudioSource.clip.length;
        }
    }

    public int NotesSelected
    {
        get
        {
            return SelectionController.SelectedObjects.Where(x => x is BeatmapNote).Count();
        }
    }

    public float NPSselected
    {
        get
        {
            List<BeatmapObject> sel = SelectionController.SelectedObjects.OrderBy(it => it._time).ToList();
            float beatTimeDiff = sel.Last()._time - sel.First()._time;
            float secDiff = atsc.GetSecondsFromBeat(beatTimeDiff);

            return NotesSelected / secDiff;
        }
    }

    public int ObstacleCount
    {
        get
        {
            return obstacles.LoadedObjects.Count;
        }
    }

    public int EventCount
    {
        get
        {
            return events.LoadedObjects.Count;
        }
    }

    public int BPMCount
    {
        get
        {
            return BeatSaberSongContainer.Instance.map._BPMChanges.Count;
        }
    }

    public int SelectedCount
    {
        get
        {
            return SelectionController.SelectedObjects.Count();
        }
    }

    public float OverallSPS
    {
        get
        {
            return swingsPerSecond.Total.Overall;
        }
    }

    [HideInInspector]
    public int hours, minutes, seconds = 0;
}
