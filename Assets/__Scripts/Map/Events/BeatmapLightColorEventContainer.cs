using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// It's strange to inherit from <see cref="BeatmapEventContainer"/>, but I need to use those shader properties to reuse <see cref="EventAppearanceSO"/>
/// </summary>
public class BeatmapLightColorEventContainer : BeatmapEventContainer
{
    public BeatmapLightColorEvent ColorEventData;
    public LightColorEventsContainer ColorEventsContainer;
    [SerializeField] GameObject extraNote;
    private List<BeatmapLightColorEventContainer> extraNotes = new List<BeatmapLightColorEventContainer>();

    public override BeatmapObject ObjectData { get => ColorEventData; set => ColorEventData = (BeatmapLightColorEvent)value; }

    public override void UpdateGridPosition()
    {
        var stackList = ColorEventsContainer.GetBetween(ColorEventData.Time - 1e-3f, ColorEventData.Time + 1e-3f)
            .Cast<BeatmapLightColorEvent>()
            .Where(x => x.Group == ColorEventData.Group)
            .ToList();
        int y = stackList.IndexOf(ColorEventData);
        transform.localPosition = new Vector3(
            ColorEventsContainer.platformDescriptor.GroupIdToLaneIndex(ColorEventData.Group) + 0.5f,
            (ColorEventsContainer.containersUP ? 1 : -1) * (y + 0.5f),
            ColorEventData.Time * EditorScaleController.EditorScale
            );
    }

    public static BeatmapLightColorEventContainer SpawnLightColorEvent(LightColorEventsContainer lightEventsContainer, BeatmapLightColorEvent data,
        ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapLightColorEventContainer>();
        container.ColorEventData = data;
        container.ColorEventsContainer = lightEventsContainer;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }


    public void SpawnEventDatas(EventAppearanceSO so, EventsContainer eventsContainer)
    {
        var eb = ColorEventData.EventBoxes[0];
        int i = 1, j = i - 1;
        for (; i < eb.EventDatas.Count; ++i, ++j)
        {
            if (j >= extraNotes.Count)
            {
                var note = Instantiate(extraNote, transform);
                var noteCon = note.GetComponent<BeatmapLightColorEventContainer>();
                noteCon.gameObject.SetActive(false);
                noteCon.Setup();
                extraNotes.Add(noteCon);
            }
            extraNotes[j].SafeSetActive(true);
            var con = extraNotes[j];
            con.ColorEventData = ColorEventData;
            var time = ColorEventData.Beat + eb.EventDatas[i].AddedBeat;
            so.SetLightColorEventAppearance(con, eventsContainer.AllBoostEvents.FindLast(x => x.Time <= time)?.Value == 1, i);
            con.transform.localScale = transform.localScale; // by default, it will be 0.75 size of main note
            con.transform.localPosition = new Vector3(
                0,
                0,
                eb.EventDatas[i].AddedBeat * EditorScaleController.EditorScale / transform.localScale.z
                );
        }
        for (; j < extraNotes.Count; ++j)
        {
            extraNotes[j].gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Infer the raycasted object's idx in the list of <see cref="BeatmapLightColorEventData"/>. return 0 by default
    /// </summary>
    public int GetRaycastedIdx()
    {
        var hit = GlobalIntersectionCache.firstHit;
        if (hit == null) return 0;
        var con = hit.GetComponentInParent<BeatmapObjectContainer>();
        for (int i = 0; i < extraNotes.Count && extraNotes[i].gameObject.activeSelf; ++i)
        {
            if (extraNotes[i] == con) return i + 1;
        }
        return 0;
    }
}
