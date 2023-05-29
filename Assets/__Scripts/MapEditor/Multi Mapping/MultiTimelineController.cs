using System.Collections.Generic;
using UnityEngine;

public class MultiTimelineController
{
    private readonly MultiNetListener multiNetListener;
    private readonly BookmarkManager bookmarkManager;
    private readonly RectTransform timelineCanvas;
    private readonly MultiTimelineContainer prefab;
    private readonly float songLength;

    private readonly Dictionary<MapperIdentityPacket, MultiTimelineContainer> activeContainers = new Dictionary<MapperIdentityPacket, MultiTimelineContainer>();

    private float previousCanvasWidth;

    public MultiTimelineController(MultiNetListener multiNetListener, BookmarkManager bookmarkManager)
    {
        this.multiNetListener = multiNetListener;
        this.bookmarkManager = bookmarkManager;
        timelineCanvas = bookmarkManager.GetComponentInParent<Canvas>().transform as RectTransform;
        songLength = bookmarkManager.Atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.LoadedSong.length);
        prefab = Resources.Load<MultiTimelineContainer>("Timeline Mapper");
    }

    public void UpdatePose(MapperIdentityPacket identity, MapperPosePacket pose)
    {
        if (!activeContainers.TryGetValue(identity, out var container))
        {
            container = Object.Instantiate(prefab, bookmarkManager.transform);
            container.Init(this, identity);
            activeContainers.Add(identity, container);
        }

        container.RefreshPosition(pose, timelineCanvas.sizeDelta.x + BookmarkManager.CanvasWidthOffset, songLength);
    }

    public void DisconnectMapper(MapperIdentityPacket identity)
    {
        if (activeContainers.TryGetValue(identity, out var container))
        {
            Object.Destroy(container);
            activeContainers.Remove(identity);
        }
    }

    public void ManualUpdate()
    {
        if (previousCanvasWidth != timelineCanvas.sizeDelta.x)
        {
            previousCanvasWidth = timelineCanvas.sizeDelta.x;
            multiNetListener.UpdateCachedPoses();
        }
    }

    public void JumpTo(MapperPosePacket pose)
    {
        bookmarkManager.Tipc.PointerDown();
        bookmarkManager.Atsc.MoveToSongBpmTime(pose.SongPosition);
        bookmarkManager.Tipc.PointerUp();
    }
}
