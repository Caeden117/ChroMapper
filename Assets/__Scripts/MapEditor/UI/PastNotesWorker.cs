using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PastNotesWorker : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private NotesContainer notesContainer;
    [SerializeField] private GameObject gridNotePrefab;

    private Transform notes;
    private Canvas _canvas;

    private readonly float _tolerance = 0.25f;
    private readonly float _gridSize = 25f;

    private Dictionary<GameObject, Image> InstantiatedNotes = new Dictionary<GameObject, Image>();

    private void Start()
    {
        _canvas = GetComponent<Canvas>();
        notes = transform.GetChild(0);
        //InvokeRepeating(nameof(UpdateUI), 1f, 0.25f); //Saving this for later
    }

    private void OnDisable()
    {
        //CancelInvoke(nameof(UpdateUI));
    }

    private bool _firstLoad = true;

    private void LateUpdate() //This could be changed to a InvokeRepeating method to save processing.
    {
        float scale = 0f;
        if (!_firstLoad)
        {
            scale = Settings.Instance.PastNotesGridScale;
            _canvas.enabled = scale != 0f;
            transform.localScale = Vector3.one * (scale + 0.25f);
            if (scale == 0f) return;
        }
        _firstLoad = false;
        
        try
        {
            foreach (Transform child in notes) child.gameObject.SetActive(false);

            IEnumerable<BeatmapNoteContainer> lastNotes = notesContainer.LoadedContainers.Where(x => x.objectData._time < atsc.CurrentBeat)
                .OrderByDescending(x => x.objectData._time).Cast<BeatmapNoteContainer>().DistinctBy(x => x.mapNoteData._type);
            foreach (BeatmapNoteContainer o in lastNotes) //Pulls all notes on the same grid line
            {
                if (o.mapNoteData._type == BeatmapNote.NOTE_TYPE_BOMB) continue;
                float gridPosX = o.mapNoteData._lineIndex, gridPosY = o.mapNoteData._lineLayer;

                if (gridPosX >= 1000) gridPosX = gridPosX / 1000 - 1f;
                else if (gridPosX <= -1000f) gridPosX = gridPosX / 1000f + 1f;
                
                if (gridPosY >= 1000) gridPosY = gridPosX / 1000f + 0.5f; //todo: Fix this so it works!
                else if (gridPosY <= -1000f) gridPosY = gridPosY / 1000f - 0.5f;//todo: Fix this so it works!

                GameObject g; //Instead of instantiating new objects every frame (Bad on performance), we are instead using a pooled system to use
                Image img; //Already existing notes, and only create ones we need.
                if (InstantiatedNotes.Any(x => !x.Key.activeSelf))
                {
                    g = InstantiatedNotes.First(x => !x.Key.activeSelf).Key;
                    img = InstantiatedNotes[g];
                    g.SetActive(true);
                    foreach (Transform child in g.transform) child.gameObject.SetActive(true);
                }
                else
                {
                    g = Instantiate(gridNotePrefab, notes.transform, true);
                    img = g.GetComponent<Image>();
                    InstantiatedNotes.Add(g, img);
                }

                var transform1 = img.transform;
                transform1.localPosition = new Vector3(_gridSize*gridPosX,_gridSize*gridPosY,1);
                float sc = scale/10f + .06f;
                transform1.localScale = new Vector3(sc,sc); //I have to do this because the UI scaling is weird

                //transform1.rotation = o.transform.rotation; //This code breaks when using 360 maps; use local rotation instead.
                transform1.localEulerAngles = Vector3.forward * o.transform.localEulerAngles.z; //Sets the rotation of the image to match the same rotation as the block
                img.color = o.transform.GetChild(0).GetComponent<MeshRenderer>().materials.FirstOrDefault(x => x.shader.name.EndsWith("Lit")).color; //Sets the color to the same color the block is

                bool dotEnabled = o.transform.GetChild(1).GetComponent<SpriteRenderer>().enabled; //Checks to see if the Dot is visible on the block

                if (dotEnabled) g.transform.GetChild(0).gameObject.SetActive(false);
                else g.transform.GetChild(1).gameObject.SetActive(false);
                img.enabled = true;
            }
        }
        catch (NullReferenceException) {}
    }
}
