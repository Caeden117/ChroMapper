using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BeatmapLightEventContainerBase<TBo, TEb, TEbd, TBoc, TBocc, TLightEvent> : BeatmapEventContainer
    where TBo: BeatmapLightEventBase<TEb, TEbd>
    where TEb: BeatmapLightEventBoxBase<TEbd>, new()
    where TEbd: BeatmapLightEventBoxDataBase, new()
    where TBoc: BeatmapLightEventContainerBase<TBo, TEb, TEbd, TBoc, TBocc, TLightEvent>
    where TBocc: LightEventsContainerCollectionBase<TBo, TEb, TEbd, TBoc, TBocc, TLightEvent>
    where TLightEvent: ILightEventV3
{
    public TBo LightEventData;
    public TBocc LightEventsContainer;
    [SerializeField] protected GameObject ExtraNote;
    protected List<TBoc> ExtraNotes = new List<TBoc>();

    public override BeatmapObject ObjectData { get => LightEventData; set => LightEventData = (TBo)value; }

    public override void UpdateGridPosition()
    {
        var stackList = LightEventsContainer.GetBetween(LightEventData.Time - 1e-3f, LightEventData.Time + 1e-3f)
            .Cast<TBo>()
            .Where(x => x.Group == LightEventData.Group)
            .ToList();
        int y = stackList.IndexOf(LightEventData);
        transform.localPosition = new Vector3(
            LightEventsContainer.platformDescriptor.GroupIdToLaneIndex(LightEventData.Group) + 0.5f,
            (LightEventsContainer.containersUP ? 1 : -1) * (y + 0.5f),
            LightEventData.Time * EditorScaleController.EditorScale
            );
    }

    public static TBoc SpawnLightEvent(TBocc lightEventsContainer, TBo data,
        ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        var container = Instantiate(prefab).GetComponent<TBoc>();
        container.LightEventData = data;
        container.LightEventsContainer = lightEventsContainer;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }

    public void SpawnEventDatas(EventAppearanceSO so)
    {
        var eb = LightEventData.EventBoxes[0];
        int i = 1, j = i - 1;
        for (; i < eb.EventDatas.Count; ++i, ++j)
        {
            if (j >= ExtraNotes.Count)
            {
                var note = Instantiate(ExtraNote, transform);
                var noteCon = note.GetComponent<TBoc>();
                noteCon.gameObject.SetActive(false);
                noteCon.Setup();
                ExtraNotes.Add(noteCon);
            }
            ExtraNotes[j].SafeSetActive(true);
            var con = ExtraNotes[j];
            con.LightEventData = LightEventData;
            var time = LightEventData.Beat + eb.EventDatas[i].AddedBeat;
            SetLightEventAppearance(so, con, time, i);
            con.transform.localScale = transform.localScale; // by default, it will be 0.75 size of main note
            con.transform.localPosition = new Vector3(
                0,
                0,
                eb.EventDatas[i].AddedBeat * EditorScaleController.EditorScale / transform.localScale.z
                );
        }
        for (; j < ExtraNotes.Count; ++j)
        {
            ExtraNotes[j].gameObject.SetActive(false);
        }
    }

    public abstract void SetLightEventAppearance(EventAppearanceSO so, TBoc con, float time, int i);

    /// <summary>
    /// Infer the raycasted object's idx in the list of <see cref="BeatmapLightColorEventData"/>. return 0 by default
    /// </summary>
    public int GetRaycastedIdx()
    {
        var hit = GlobalIntersectionCache.firstHit;
        if (hit == null) return 0;
        var con = hit.GetComponentInParent<BeatmapObjectContainer>();
        for (int i = 0; i < ExtraNotes.Count && ExtraNotes[i].gameObject.activeSelf; ++i)
        {
            if (ExtraNotes[i] == con) return i + 1;
        }
        return 0;
    }
}
