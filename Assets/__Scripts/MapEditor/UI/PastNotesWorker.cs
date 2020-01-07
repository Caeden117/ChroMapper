using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PastNotesWorker : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private NotesContainer notesContainer;
    private Canvas _canvas;

    private Image[][] grid;
    private Image[][] gridArrow;
    private Image[][] gridDot;

    private readonly float[] noteGridX = {-1.5f, -0.5f, 0.5f, 1.5f};
    private readonly float[] noteGridY = {2.5f, 1.5f, 0.5f};

    private readonly float _tolerance = 0.25f;
    
    private void Start()
    {
        _canvas = GetComponent<Canvas>();
        List<Image> imgList = new List<Image>();
        List<Image> imgArrowList = new List<Image>();
        List<Image> imgDotList = new List<Image>();

        foreach (Image i in GetComponentsInChildren<Image>().ToList())
        {
            if (i.name != name)
            {
                switch (i.name)
                {
                    case "Arrow":
                        imgArrowList.Add(i);
                        break;
                    case "Dot":
                        imgDotList.Add(i);
                        break;
                    default:
                        imgList.Add(i);
                        break;
                }
            }
        }

        grid = new Image[noteGridY.Length][]; //You have to do this or else it gets mad.
        gridArrow = new Image[noteGridY.Length][]; //You have to do this or else it gets mad.
        gridDot = new Image[noteGridY.Length][]; //You have to do this or else it gets mad.

        int check = 0;
        while (check < noteGridY.Length)
        {
            grid[check] = imgList.GetRange(check*4,noteGridX.Length).ToArray();
            gridArrow[check] = imgArrowList.GetRange(check*4,noteGridX.Length).ToArray();
            gridDot[check] = imgDotList.GetRange(check*4,noteGridX.Length).ToArray();
            check++;
        }

        //InvokeRepeating(nameof(UpdateUI), 1f, 0.25f); //Saving this for later
    }

    private void OnDisable()
    {
        //CancelInvoke(nameof(UpdateUI));
    }

    private bool _firstLoad = true;

    private void LateUpdate() //This could be changed to a InvokeRepeating method to save possessing.
    {
        if (!_firstLoad)
        {
            float scale = Settings.Instance.PastNotesGridScale;
            _canvas.enabled = scale != 0f;
            transform.localScale = Vector3.one * (scale + 0.25f);
            if (scale == 0f) return;
        }
        _firstLoad = false;
        try
        {
            for (int gg = 0; gg<grid.Length; gg++) //Clears the grid
            {
                for (int ggg = 0; ggg<grid[gg].Length; ggg++)
                {
                    grid[gg][ggg].transform.rotation = new Quaternion(0,0,0, 0);
                    grid[gg][ggg].color = Color.clear;

                    gridArrow[gg][ggg].enabled = false;
                    gridDot[gg][ggg].enabled = false;
                }
            }
            
            float f = notesContainer.LoadedContainers.LastOrDefault(x => x.objectData._time < atsc.CurrentBeat).objectData._time; //Pulls Closest note behind main grid
            foreach (BeatmapObjectContainer o in notesContainer.LoadedContainers.Where(x => x.objectData._time == f).ToList()) //Pulls all notes on the same grid line
            {
                Vector3 pos = o.transform.position;
                int gridPosX = 0, gridPosY = 0;
                
                foreach (float y in noteGridY)
                {
                    if (Math.Abs(pos.y - y) < _tolerance) break;
                    gridPosY++;
                }
                
                foreach (float x in noteGridX)
                {
                    if (Math.Abs(pos.x - x) < _tolerance) break;
                    gridPosX++;
                }

                Image img = grid[gridPosY][gridPosX];
                img.transform.rotation = o.transform.rotation; //Sets the rotation of the image to match the same rotation as the block
                img.color = o.transform.GetChild(0).GetComponent<MeshRenderer>().materials.FirstOrDefault(x => x.shader.name.EndsWith("Lit")).color; //Sets the color to the same color the block is

                bool dotEnabled = o.transform.GetChild(1).GetComponent<SpriteRenderer>().enabled; //Checks to see if the Dot is visible on the block

                Image arrow = gridArrow[gridPosY][gridPosX];
                Image dot = gridDot[gridPosY][gridPosX];

                arrow.enabled = !dotEnabled;
                dot.enabled = dotEnabled;
            }
        }
        catch (NullReferenceException) {}
    }
}
