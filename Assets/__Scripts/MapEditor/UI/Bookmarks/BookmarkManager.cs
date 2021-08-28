using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class BookmarkManager : MonoBehaviour, CMInput.IBookmarksActions
{
    [SerializeField] private GameObject bookmarkContainerPrefab;
    [FormerlySerializedAs("atsc")] public AudioTimeSyncController Atsc;
    [FormerlySerializedAs("tipc")] public TimelineInputPlaybackController Tipc;

    [SerializeField] private RectTransform timelineCanvas;

    // -10 twice for the distance from screen edges, -5 for half the width of one bookmark
    private readonly float canvasWidthOffset = -20f;
    internal List<BookmarkContainer> BookmarkContainers = new List<BookmarkContainer>();

    public Action BookmarksUpdated;

    private float previousCanvasWidth;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); //Wait for time
        BookmarkContainers = BeatSaberSongContainer.Instance.Map.Bookmarks.Select(bookmark =>
        {
            var container = Instantiate(bookmarkContainerPrefab, transform).GetComponent<BookmarkContainer>();
            container.name = bookmark.Name;
            container.Init(this, bookmark);
            container.RefreshPosition(timelineCanvas.sizeDelta.x + canvasWidthOffset);
            return container;
        }).OrderBy(it => it.Data.Time).ToList();
    }

    private void LateUpdate()
    {
        if (previousCanvasWidth != timelineCanvas.sizeDelta.x)
        {
            previousCanvasWidth = timelineCanvas.sizeDelta.x;
            foreach (var bookmark in BookmarkContainers)
                bookmark.RefreshPosition(timelineCanvas.sizeDelta.x + canvasWidthOffset);
        }
    }

    public void OnCreateNewBookmark(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "bookmark.dialog", HandleNewBookmarkName,
                "bookmark.dialog.default");
        }
    }

    public void OnNextBookmark(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnNextBookmark();
    }

    public void OnPreviousBookmark(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnPreviousBookmark();
    }

    internal void HandleNewBookmarkName(string res)
    {
        if (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) return;
        var newBookmark = new BeatmapBookmark(Atsc.CurrentBeat, res);
        var container = Instantiate(bookmarkContainerPrefab, transform).GetComponent<BookmarkContainer>();
        container.name = newBookmark.Name;
        container.Init(this, newBookmark);
        container.RefreshPosition(timelineCanvas.sizeDelta.x + canvasWidthOffset);

        BookmarkContainers = BookmarkContainers.Append(container).OrderBy(it => it.Data.Time).ToList();
        BeatSaberSongContainer.Instance.Map.Bookmarks = BookmarkContainers.Select(x => x.Data).ToList();
        BookmarksUpdated.Invoke();
    }

    internal void DeleteBookmark(BookmarkContainer container)
    {
        BookmarkContainers.Remove(container);
        BeatSaberSongContainer.Instance.Map.Bookmarks = BookmarkContainers.Select(x => x.Data).ToList();
        BookmarksUpdated.Invoke();
    }

    internal void OnNextBookmark()
    {
        var bookmark = BookmarkContainers.Find(x => x.Data.Time > Atsc.CurrentBeat);
        if (bookmark != null)
        {
            Tipc.PointerDown(); // slightly weird but it works
            Atsc.MoveToTimeInBeats(bookmark.Data.Time);
            Tipc.PointerUp();
        }
    }

    internal void OnPreviousBookmark()
    {
        var bookmark = BookmarkContainers.LastOrDefault(x => x.Data.Time < Atsc.CurrentBeat);
        if (bookmark != null)
        {
            Tipc.PointerDown();
            Atsc.MoveToTimeInBeats(bookmark.Data.Time);
            Tipc.PointerUp();
        }
    }
}
