using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.CompilerServices;

public class BookmarkManager : MonoBehaviour, CMInput.IBookmarksActions
{
    internal List<BookmarkContainer> bookmarkContainers = new List<BookmarkContainer>();
    [SerializeField] private GameObject bookmarkContainerPrefab;
    public AudioTimeSyncController atsc;

    [SerializeField] private RectTransform timelineCanvas;

    private float previousCanvasWidth = 0;

    // -10 twice for the distance from screen edges, -5 for half the width of one bookmark
    private readonly float CANVAS_WIDTH_OFFSET = -20f;
    
    public Action OnBookmarksUpdated;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); //Wait for time
        bookmarkContainers = BeatSaberSongContainer.Instance.map._bookmarks.Select(bookmark =>
        {
            BookmarkContainer container = Instantiate(bookmarkContainerPrefab, transform).GetComponent<BookmarkContainer>();
            container.name = bookmark._name;
            container.Init(this, bookmark);
            container.RefreshPosition(timelineCanvas.sizeDelta.x + CANVAS_WIDTH_OFFSET);
            return container;
        }).OrderBy(it => it.data._time).ToList();
    }

    public void OnCreateNewBookmark(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "bookmark.dialog", HandleNewBookmarkName, "bookmark.dialog.default");
        }
    }

    internal void HandleNewBookmarkName(string res)
    {
        if (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) return;
        var newBookmark = new BeatmapBookmark(atsc.CurrentBeat, res);
        var container = Instantiate(bookmarkContainerPrefab, transform).GetComponent<BookmarkContainer>();
        container.name = newBookmark._name;
        container.Init(this, newBookmark);
        container.RefreshPosition(timelineCanvas.sizeDelta.x + CANVAS_WIDTH_OFFSET);

        bookmarkContainers = bookmarkContainers.Append(container).OrderBy(it => it.data._time).ToList();
        BeatSaberSongContainer.Instance.map._bookmarks = bookmarkContainers.Select(x => x.data).ToList();
        OnBookmarksUpdated.Invoke();
    }

    internal void DeleteBookmark(BookmarkContainer container)
    {
        bookmarkContainers.Remove(container);
        BeatSaberSongContainer.Instance.map._bookmarks = bookmarkContainers.Select(x => x.data).ToList();
        OnBookmarksUpdated.Invoke();
    }

    public void OnNextBookmark(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnNextBookmark();
    }

    internal void OnNextBookmark()
    {
        BookmarkContainer bookmark = bookmarkContainers.Find(x => x.data._time > atsc.CurrentBeat);
        if (bookmark != null) atsc.MoveToTimeInBeats(bookmark.data._time);
    }

    public void OnPreviousBookmark(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnPreviousBookmark();
    }

    internal void OnPreviousBookmark()
    {
        BookmarkContainer bookmark = bookmarkContainers.LastOrDefault(x => x.data._time < atsc.CurrentBeat);
        if (bookmark != null) atsc.MoveToTimeInBeats(bookmark.data._time);
    }

    private void LateUpdate()
    {
        if (previousCanvasWidth != timelineCanvas.sizeDelta.x)
        {
            previousCanvasWidth = timelineCanvas.sizeDelta.x;
            foreach (BookmarkContainer bookmark in bookmarkContainers)
            {
                bookmark.RefreshPosition(timelineCanvas.sizeDelta.x + CANVAS_WIDTH_OFFSET);
            }
        }
    }
}
