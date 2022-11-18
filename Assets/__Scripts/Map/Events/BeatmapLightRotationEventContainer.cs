using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeatmapLightRotationEventContainer : BeatmapEventContainer
{
    public BeatmapLightRotationEvent RotationEventData;
    public LightRotationEventsContainer RotationEventsContainer;
    [SerializeField] private GameObject extraNote;
    [SerializeField] private MeshRenderer axisMark;
    private List<BeatmapLightRotationEventContainer> extraNotes = new List<BeatmapLightRotationEventContainer>();

    public override BeatmapObject ObjectData { get => RotationEventData; set => RotationEventData = (BeatmapLightRotationEvent)value; }

    public override void UpdateGridPosition()
    {
        var stackList = RotationEventsContainer.GetBetween(RotationEventData.Time - 1e-3f, RotationEventData.Time + 1e-3f)
            .Cast<BeatmapLightRotationEvent>()
            .Where(x => x.Group == RotationEventData.Group)
            .ToList();
        int y = stackList.IndexOf(RotationEventData);
        transform.localPosition = new Vector3(
            RotationEventsContainer.platformDescriptor.GroupIdToLaneIndex(RotationEventData.Group) + 0.5f,
            (RotationEventsContainer.containersUP ? 1 : -1) * (y + 0.5f),
            RotationEventData.Time * EditorScaleController.EditorScale
            );
    }

    public static BeatmapLightRotationEventContainer SpawnLightRotationEvent(LightRotationEventsContainer rotationEventsContainer, BeatmapLightRotationEvent data,
        ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapLightRotationEventContainer>();
        container.RotationEventData = data;
        container.RotationEventsContainer = rotationEventsContainer;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }

    public void SpawnEventDatas(EventAppearanceSO so)
    {
        var eb = RotationEventData.EventBoxes[0];
        int i = 1, j = i - 1;
        for (; i < eb.EventDatas.Count; ++i, ++j)
        {
            if (j >= extraNotes.Count)
            {
                var note = Instantiate(extraNote, transform);
                var noteCon = note.GetComponent<BeatmapLightRotationEventContainer>();
                noteCon.gameObject.SetActive(false);
                noteCon.Setup();
                extraNotes.Add(noteCon);
            }
            extraNotes[j].SafeSetActive(true);
            var con = extraNotes[j];
            con.RotationEventData = RotationEventData;
            so.SetLightRotationEventAppearance(con, i);
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

    public void SetRotationAxisAppearance(int axis)
    {
        axisMark.transform.localRotation = Quaternion.Euler(
            axis == 1 ? 90 : 0,
            axis == 0 ? 90 : 0,
            0
            );
    }
}
